# Copilot Instructions

## Project Guidelines
- User pointed out that `CustomClaimsTransformation` executes on every authenticated request in this solution.

## Testing Guidelines
- Use `TestServiceBase<PermissionsTagHelper>` in unit tests so `IFeatureManager` is auto-injected.
- Use `Sut` instead of manually instantiating `PermissionsTagHelper`; only get `IFeatureManager` mock when needed.
- Remove `TaskBoolShouldBeExtensions` and use `async Task` tests for async methods.