import axios from 'axios';

export const employeeApi = axios.create({ baseURL: 'http://localhost:3000' });
export const absenceApi = axios.create({ baseURL: 'http://localhost:3001' });

[employeeApi, absenceApi].forEach((api) => {
    api.interceptors.request.use((config) => {
        const token = localStorage.getItem('admin_token');
        if (token) config.headers.Authorization = `Bearer ${token}`;
        return config;
    });
});