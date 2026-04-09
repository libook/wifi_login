# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0] - 2024-01-01

### Added
- Initial release
- Background network monitoring
- Multi-SSID configuration support
- Encrypted password storage using Windows DPAPI
- Automatic Captive Portal detection
- WebView2-based automatic login
- System tray integration
- Internationalization support (English/Chinese)
- Optional logging functionality
- Auto-start on boot support
- Test login feature
- CSS selector configuration for custom login pages

### Security
- Password encryption using Windows DPAPI (ProtectedData)
- Only the current user can decrypt stored passwords

## Version History

| Version | Date | Description |
|---------|------|-------------|
| 1.0.0 | 2024-01-01 | Initial release with core features |
