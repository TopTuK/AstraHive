import { api } from './api';

export const fetchWorkspaces = () => api.get('/workspaces').then((response) => response.data);

export const fetchMembers = (workspaceId) =>
    api.get(`/workspaces/${workspaceId}/members`).then((response) => response.data);

export const addMember = ({ workspaceId, email, role }) =>
    api.post(`/workspaces/${workspaceId}/members`, { email, role }).then((response) => response.data);

export const changeMemberRole = ({ workspaceId, userId, role }) =>
    api.put(`/workspaces/${workspaceId}/members/${userId}`, { role }).then((response) => response.data);

export const removeMember = ({ workspaceId, userId }) =>
    api.delete(`/workspaces/${workspaceId}/members/${userId}`);

export const createWorkspace = ({ name }) => api.post('/workspaces', { name }).then((response) => response.data);
