import { api } from './api';

export const fetchProfile = () => api.get('/profile').then((response) => response.data);

export const updateProfile = ({ firstName, lastName }) =>
    api.put('/profile', { firstName, lastName }).then((response) => response.data);
