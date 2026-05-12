# Copilot Instructions

## Project Guidelines
- Keep UserStatus enum as native enum type without string conversion. Let Npgsql handle enum serialization natively through its enum mapping in Startup.cs, rather than using EnumToStringConverter.