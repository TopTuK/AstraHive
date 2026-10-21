namespace AH.Application.Abstractions;

// Names live in FeatureFlags. An unknown flag, or a flag service that cannot be reached, reads as off.
public interface IFeatureFlags
{
    bool IsEnabled(string name);
}
