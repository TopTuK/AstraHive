using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using AH.Infrastructure.Certificates;

// Creates the root CA that issues AstraHive client certificates, and the Global Administrator's client
// certificate signed by it. WebApi only ever receives the root's public certificate; IdentityService gets the
// root key to issue member certificates, and registers the Global Administrator's certificate on startup.
//
//   dev   Run by the AstraHive.AppHost Debug build (EnsureDevCertificates target): a per-machine dev root and
//         client certificate, installed into CurrentUser\My so the browser offers it. Idempotent.
//   init  Run by hand once per production deployment: writes the files to put into the deployment parameters.

const string DevRootSubject = "CN=AstraHive Dev Root CA, O=AstraHive";
const string ProductionRootSubject = "CN=AstraHive Root CA, O=AstraHive";
const string RootFileName = "root.pem";
const string RootKeyFileName = "root.key.pem";
// Firefox keeps its own store: import <email>.pfx there with this password.
const string DevPfxPassword = "astrahive";
const string PasswordAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";

var options = Options.Parse(args);
if (options is null)
{
    Console.Error.WriteLine("""
        Usage:
          AH.CertTool dev  --out <dir> --email <email> [--name <display name>]
          AH.CertTool init --out <dir> --email <email> [--name <display name>] [--root-years <n=10>] [--client-years <n=2>]
        """);
    return 1;
}

Directory.CreateDirectory(options.OutputDirectory);
return options.Command switch
{
    "dev" => RunDev(options),
    _ => RunInit(options),
};

static int RunDev(Options options)
{
    var rootPath = Path.Combine(options.OutputDirectory, RootFileName);
    var rootKeyPath = Path.Combine(options.OutputDirectory, RootKeyFileName);
    var pfxPath = Path.Combine(options.OutputDirectory, $"{options.Email}.pfx");
    var pemPath = Path.Combine(options.OutputDirectory, $"{options.Email}.pem");
    var now = TimeProvider.System.GetUtcNow();

    using var root = File.Exists(rootPath) && File.Exists(rootKeyPath)
        ? X509Certificate2.CreateFromPemFile(rootPath, rootKeyPath)
        : WriteRoot(DevRootSubject, rootPath, rootKeyPath, now, options.RootYears);

    using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
    store.Open(OpenFlags.ReadWrite);

    var issued = IssuedTo(store, options.Email);
    if (File.Exists(pfxPath) && issued.FirstOrDefault(c => IsValidUnder(c, root, now)) is { } existing)
    {
        // Folders from before the bootstrap registration have no public PEM yet.
        if (!File.Exists(pemPath))
        {
            File.WriteAllText(pemPath, existing.ExportCertificatePem());
        }

        Console.WriteLine($"AH.CertTool: client certificate for {options.Email} already installed ({existing.Thumbprint}).");
        return 0;
    }

    // Copies from an earlier run (lost pfx, or a regenerated root) would only clutter the browser's picker.
    foreach (var stale in issued)
    {
        store.Remove(stale);
    }

    using var client = ClientCertificateFactory.CreateClient(root, options.Email, options.Name, now, options.ClientYears);
    var pfx = ClientCertificateFactory.ExportPfx(client, DevPfxPassword);
    File.WriteAllBytes(pfxPath, pfx);
    File.WriteAllText(pemPath, client.ExportCertificatePem());

    using var persisted = X509CertificateLoader.LoadPkcs12(
        pfx,
        DevPfxPassword,
        X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.UserKeySet);
    store.Add(persisted);

    Console.WriteLine($"AH.CertTool: installed client certificate for {options.Email} into CurrentUser\\My ({persisted.Thumbprint}).");
    Console.WriteLine($"AH.CertTool: root CA at {rootPath}.");
    return 0;
}

static int RunInit(Options options)
{
    var rootPath = Path.Combine(options.OutputDirectory, RootFileName);
    var rootKeyPath = Path.Combine(options.OutputDirectory, RootKeyFileName);
    var pfxPath = Path.Combine(options.OutputDirectory, $"{options.Email}.pfx");
    var pemPath = Path.Combine(options.OutputDirectory, $"{options.Email}.pem");

    // A second run would silently replace the root every issued certificate chains to.
    var existing = new[] { rootPath, rootKeyPath, pfxPath, pemPath }.Where(File.Exists).ToList();
    if (existing.Count > 0)
    {
        Console.Error.WriteLine($"AH.CertTool: refusing to overwrite {string.Join(", ", existing)}. Use an empty --out directory.");
        return 2;
    }

    var now = TimeProvider.System.GetUtcNow();
    using var root = WriteRoot(ProductionRootSubject, rootPath, rootKeyPath, now, options.RootYears);
    using var client = ClientCertificateFactory.CreateClient(root, options.Email, options.Name, now, options.ClientYears);
    var password = RandomNumberGenerator.GetString(PasswordAlphabet, 20);
    File.WriteAllBytes(pfxPath, ClientCertificateFactory.ExportPfx(client, password));
    File.WriteAllText(pemPath, client.ExportCertificatePem());

    Console.WriteLine($"""

        Root CA:        {root.Subject}, valid until {root.NotAfter:yyyy-MM-dd}
                        {rootPath}
                        {rootKeyPath}   <- private key: keep offline, give to IdentityService only
        Global Admin:   {options.Email} ({options.Name}), valid until {client.NotAfter:yyyy-MM-dd}
                        {pfxPath}   (password: {password})
                        {pemPath}

        Deployment parameters (AstraHive.AppHost, publish mode):
          certificate-root          = contents of {RootFileName}
          certificate-root-key      = contents of {RootKeyFileName}  (secret)
          global-admin-email        = {options.Email}
          global-admin-name         = {options.Name}
          global-admin-certificate  = contents of {Path.GetFileName(pemPath)}

        Hand {Path.GetFileName(pfxPath)} and its password to the Global Administrator. The password is shown only now.
        """);
    return 0;
}

static X509Certificate2 WriteRoot(string subject, string rootPath, string rootKeyPath, DateTimeOffset now, int validYears)
{
    var root = ClientCertificateFactory.CreateRoot(subject, now, validYears);
    File.WriteAllText(rootPath, root.ExportCertificatePem());
    File.WriteAllText(rootKeyPath, root.GetECDsaPrivateKey()!.ExportPkcs8PrivateKeyPem());
    Console.WriteLine($"AH.CertTool: created root CA {root.Subject} ({root.Thumbprint}).");
    return root;
}

static List<X509Certificate2> IssuedTo(X509Store store, string email) =>
    [.. store.Certificates.Where(c => c.Issuer == DevRootSubject
        && string.Equals(c.GetNameInfo(X509NameType.EmailName, false), email, StringComparison.OrdinalIgnoreCase))];

static bool IsValidUnder(X509Certificate2 certificate, X509Certificate2 root, DateTimeOffset now)
{
    if (certificate.NotAfter.ToUniversalTime() <= now.UtcDateTime)
    {
        return false;
    }

    using var chain = new X509Chain();
    chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
    chain.ChainPolicy.CustomTrustStore.Add(root);
    chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
    return chain.Build(certificate);
}

internal sealed record Options(string Command, string OutputDirectory, string Email, string Name, int RootYears, int ClientYears)
{
    public static Options? Parse(string[] args)
    {
        if (args.Length == 0 || args[0] is not ("dev" or "init"))
        {
            return null;
        }

        string? output = null, email = null, name = null;
        int rootYears = 10, clientYears = 2;
        for (var i = 1; i + 1 < args.Length; i += 2)
        {
            switch (args[i])
            {
                case "--out": output = args[i + 1]; break;
                case "--email": email = args[i + 1]; break;
                case "--name": name = args[i + 1]; break;
                case "--root-years" when int.TryParse(args[i + 1], out var years) && years > 0: rootYears = years; break;
                case "--client-years" when int.TryParse(args[i + 1], out var years) && years > 0: clientYears = years; break;
                default: return null;
            }
        }

        // Pairs only: a trailing flag without a value is a usage error.
        if (args.Length % 2 == 0 || string.IsNullOrWhiteSpace(output) || string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var trimmedEmail = email.Trim().ToLowerInvariant();
        return new Options(
            args[0],
            Path.GetFullPath(output),
            trimmedEmail,
            string.IsNullOrWhiteSpace(name) ? trimmedEmail : name.Trim(),
            rootYears,
            clientYears);
    }
}
