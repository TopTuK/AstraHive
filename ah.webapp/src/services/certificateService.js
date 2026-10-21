import { api } from './api';

const base = (workspaceId, userId) => `/workspaces/${workspaceId}/members/${userId}/certificates`;

export const fetchCertificates = ({ workspaceId, userId }) =>
    api.get(base(workspaceId, userId)).then((response) => response.data);

// Answers with the certificate and its PFX password, which the server returns only this once.
export const issueCertificate = ({ workspaceId, userId }) =>
    api.post(base(workspaceId, userId)).then((response) => response.data);

export const revokeCertificate = ({ workspaceId, userId, certificateId }) =>
    api.delete(`${base(workspaceId, userId)}/${certificateId}`).then((response) => response.data);

// A plain same-origin link: the session cookie authorizes it and the browser saves the attachment.
export const downloadUrl = ({ workspaceId, userId, certificateId }) =>
    `/api${base(workspaceId, userId)}/${certificateId}/download`;
