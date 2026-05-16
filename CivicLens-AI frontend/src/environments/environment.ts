export const environment = {
  production: false,
  apiUrl:
    (typeof window !== 'undefined' && window.location?.protocol === 'https:')
      ? 'https://localhost:7028/api'
      : 'http://localhost:5242/api',
  mapServiceUrl: 'http://localhost:8002',
  appVersion: '2.4.1',
  branding: {
    countryCode: 'TN',
    countryName: 'Tunisie',
    countryFlag: '🇹🇳'
  }
};
