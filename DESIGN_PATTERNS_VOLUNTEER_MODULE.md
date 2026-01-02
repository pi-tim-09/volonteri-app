# Design Patterns Implementation - Volunteer Module
**Student:** Daria  
**Module:** Korisnièki (Volunteer) Module  
**Date:** January 2025

---

## Summary
This document describes the implementation of **3 Design Patterns** (1x Creational, 1x Structural, 1x Behavioral) for the volunteer module, following **SOLID principles** and **OOP best practices**.

---

## 1. Creational Pattern: Factory Method ?

### Pattern: Factory Method
**Category:** Creational  
**Location:** `WebApp\Patterns\Creational\UserFactory.cs`

### Purpose:
Creates different types of users (Volunteer, Organization, Admin) without tight coupling to concrete classes.

### Implementation:
- **Interface:** `IUserFactory`, `IUserFactoryProvider`
- **Concrete Factories:** `VolunteerFactory`, `OrganizationFactory`, `AdminFactory`
- **Provider:** `UserFactoryProvider` - selects the appropriate factory based on UserRole

### Usage in Code:
```csharp
// UserService.CreateUserAsync() - Line 30
User user = _userFactoryProvider.CreateUser(
    userVm.Role,
    userVm.Email,
    userVm.FirstName,
    userVm.LastName,
    userVm.PhoneNumber);
```

### SOLID Principles Applied:
- **Single Responsibility:** Each factory creates only one type of user
- **Open/Closed:** Can add new user types without modifying existing factories
- **Dependency Inversion:** UserService depends on IUserFactoryProvider interface, not concrete factories

---

## 2. Structural Pattern: Decorator (Volunteer Profile) ?

### Pattern: Decorator
**Category:** Structural  
**Location:** `WebApp\Patterns\Structural\VolunteerProfileDecorator.cs`

### Purpose:
Adds additional behavior (logging, validation, enrichment) to volunteer profile operations dynamically.

### Implementation:
- **Interface:** `IVolunteerProfileService`
- **Base Component:** `BasicVolunteerProfileService`
- **Abstract Decorator:** `VolunteerProfileDecorator`
- **Concrete Decorators:**
  - `LoggingVolunteerProfileDecorator` - adds logging
  - `ValidatingVolunteerProfileDecorator` - adds validation
  - `EnrichedVolunteerProfileDecorator` - adds data enrichment (skills, hours)

### Usage in Code:
```csharp
// UserService.GetEnrichedVolunteerSummaryAsync() - Line 226
return await _volunteerProfileService.FormatVolunteerSummaryAsync(volunteer);
```

### DI Registration (Program.cs):
```csharp
builder.Services.AddScoped<IVolunteerProfileService>(serviceProvider =>
{
    var baseService = new BasicVolunteerProfileService();
    var validatingDecorator = new ValidatingVolunteerProfileDecorator(baseService, logger);
    var loggingDecorator = new LoggingVolunteerProfileDecorator(validatingDecorator, logger);
    var enrichedDecorator = new EnrichedVolunteerProfileDecorator(loggingDecorator, logger);
    return enrichedDecorator; // Chain of decorators
});
```

### SOLID Principles Applied:
- **Single Responsibility:** Each decorator adds only one specific behavior
- **Open/Closed:** Can add new decorators without modifying existing ones
- **Liskov Substitution:** All decorators can be used interchangeably
- **Dependency Inversion:** Decorators depend on IVolunteerProfileService interface

---

## 3. Behavioral Pattern: Observer (Volunteer Events) ?

### Pattern: Observer
**Category:** Behavioral  
**Location:** `WebApp\Patterns\Behavioral\VolunteerObserver.cs`

### Purpose:
Notifies multiple subscribers when volunteer events occur (registration, skill updates, project completion).

### Implementation:
- **Observer Interface:** `IVolunteerObserver`
- **Subject Interface:** `IVolunteerEventPublisher`
- **Concrete Subject:** `VolunteerEventPublisher` - manages observers and publishes events
- **Concrete Observers:**
  - `LoggingVolunteerObserver` - logs events
  - `StatisticsVolunteerObserver` - tracks statistics
  - `NotificationVolunteerObserver` - sends notifications

### Usage in Code:
```csharp
// UserService.CreateUserAsync() - Line 42
await _volunteerEventPublisher.NotifyVolunteerRegisteredAsync(v);

// UserService.UpdateVolunteerSkillsAsync() - Line 243
await _volunteerEventPublisher.NotifyVolunteerSkillsUpdatedAsync(volunteer, newSkills);

// UserService.RecordVolunteerProjectCompletionAsync() - Line 259
await _volunteerEventPublisher.NotifyVolunteerProjectCompletedAsync(volunteer, projectId, hoursLogged);
```

### SOLID Principles Applied:
- **Single Responsibility:** Each observer handles one specific concern (logging, statistics, notifications)
- **Open/Closed:** Can add new observers without modifying existing code
- **Dependency Inversion:** Subject depends on IVolunteerObserver interface

---

## Architecture Overview

### Pattern Separation by Module:
- **Volunteer Module (My Implementation):**
  1. Factory Method (`VolunteerFactory`)
  2. Decorator (`VolunteerProfileDecorator`)
  3. Observer (`VolunteerObserver`)

- **Shared Patterns (Used by all modules):**
  - Notification Decorator (for applications)
  - State Pattern (for application status)

### File Structure:
```
WebApp/
??? Patterns/
?   ??? Creational/
?   ?   ??? UserFactory.cs (Factory Method)
?   ??? Structural/
?   ?   ??? NotificationDecorator.cs (Shared)
?   ?   ??? VolunteerProfileDecorator.cs (Volunteer-specific) ? NEW
?   ??? Behavioral/
?       ??? ApplicationState.cs (Shared)
?       ??? VolunteerObserver.cs (Volunteer-specific) ? NEW
??? Services/
    ??? UserService.cs (Uses all patterns)
```

---

## Demo Scenarios for Professor

### Scenario 1: Factory Method
```csharp
// Create a new volunteer using factory
var volunteer = await userService.CreateUserAsync(new UserVM 
{ 
    Role = UserRole.Volunteer, 
    Email = "john@example.com" 
});
// ? VolunteerFactory is automatically selected and creates Volunteer object
// ? Observer pattern triggers: all observers notified about registration
```

### Scenario 2: Decorator Pattern
```csharp
// Get enriched volunteer profile
var summary = await userService.GetEnrichedVolunteerSummaryAsync(volunteerId);
// ? Goes through: Validation ? Logging ? Enrichment
// ? Output: "John Doe - john@example.com | Skills: C#, ASP.NET | Hours: 150"
```

### Scenario 3: Observer Pattern
```csharp
// Update volunteer skills
await userService.UpdateVolunteerSkillsAsync(volunteerId, new List<string> { "C#", "Docker" });
// ? LoggingObserver logs the event
// ? StatisticsObserver increments counter
// ? NotificationObserver sends confirmation email
```

---

## Benefits of This Implementation

### 1. **Maintainability**
- Each pattern is in its own file
- Clear separation of concerns
- Easy to understand and modify

### 2. **Extensibility**
- Can add new user types by creating new factories
- Can add new decorators without modifying existing ones
- Can add new observers without changing the publisher

### 3. **Testability**
- All patterns depend on interfaces
- Easy to mock for unit testing
- Each component can be tested in isolation

### 4. **SOLID Compliance**
- ? Single Responsibility: Each class has one reason to change
- ? Open/Closed: Open for extension, closed for modification
- ? Liskov Substitution: All implementations are interchangeable
- ? Interface Segregation: Small, focused interfaces
- ? Dependency Inversion: Depend on abstractions, not concretions

---

## Conclusion

This implementation demonstrates a **complete, production-ready** application of 3 design patterns for the volunteer module:

1. ? **Creational (Factory Method):** Creates volunteers dynamically
2. ? **Structural (Decorator):** Enhances volunteer profile operations
3. ? **Behavioral (Observer):** Notifies subscribers about volunteer events

All patterns follow **SOLID principles**, **OOP best practices**, and are **properly integrated** with dependency injection.

---

**Grade Criteria Met:**
- ? I3 (1 bod): Minimum 3 design patterns from different categories
- ? I8 (1 bod): Each team member implemented patterns in their module
- ? Bonus: SOLID principles, clean code, proper documentation
