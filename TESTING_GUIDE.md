# Testing Guide - Design Patterns Demo

## Quick Test Using Swagger

1. **Start the application**
   ```bash
   dotnet run
   ```

2. **Open Swagger UI**
   - Navigate to: `https://localhost:<port>/swagger`

3. **Test Each Pattern:**

### Test 1: Factory Method Pattern (Creational)
- **Endpoint:** `POST /api/VolunteerPatternDemo/demo-factory`
- **Click:** "Try it out" ? "Execute"
- **Expected:** Creates a volunteer using `VolunteerFactory`
- **Check logs for:** "[VOLUNTEER EVENTS] Observer subscribed" and "New volunteer registered"

### Test 2: Decorator Pattern (Structural)
- **Endpoint:** `GET /api/VolunteerPatternDemo/demo-decorator/{volunteerId}`
- **Set volunteerId:** (use ID from Test 1)
- **Click:** "Execute"
- **Expected:** Returns enriched profile with skills and hours
- **Check logs for:** "[VOLUNTEER PROFILE] Retrieving profile", "Validation passed", "Formatting summary"

### Test 3: Observer Pattern - Skills (Behavioral)
- **Endpoint:** `POST /api/VolunteerPatternDemo/demo-observer-skills/{volunteerId}`
- **Set volunteerId:** (use ID from Test 1)
- **Request body:**
  ```json
  ["C#", "ASP.NET Core", "Design Patterns"]
  ```
- **Click:** "Execute"
- **Expected:** Skills updated, all observers notified
- **Check logs for:** 
  - "[VOLUNTEER LOG] Skills updated"
  - "[VOLUNTEER STATS] Total skill updates"
  - "[VOLUNTEER NOTIFICATION] Sending skills update confirmation"

### Test 4: Observer Pattern - Completion (Behavioral)
- **Endpoint:** `POST /api/VolunteerPatternDemo/demo-observer-completion/{volunteerId}?projectId=1&hours=8`
- **Set volunteerId:** (use ID from Test 1)
- **Set projectId:** 1
- **Set hours:** 8
- **Click:** "Execute"
- **Expected:** Project completion recorded, observers notified
- **Check logs for:**
  - "[VOLUNTEER LOG] Volunteer completed project"
  - "[VOLUNTEER STATS] Total completions"
  - "[VOLUNTEER NOTIFICATION] Sending completion certificate"

### Test 5: All Patterns Combined
- **Endpoint:** `POST /api/VolunteerPatternDemo/demo-all-patterns`
- **Click:** "Try it out" ? "Execute"
- **Expected:** All 3 patterns executed in sequence
- **Check logs for:** All pattern logs from tests 1-4

---

## What to Show Your Professor

### 1. **Code Files** (show in VS Code/Visual Studio)
- `WebApp/Patterns/Creational/UserFactory.cs` - Factory Method
- `WebApp/Patterns/Structural/VolunteerProfileDecorator.cs` - Decorator
- `WebApp/Patterns/Behavioral/VolunteerObserver.cs` - Observer
- `WebApp/Services/UserService.cs` - Usage of all patterns

### 2. **Live Demo via Swagger**
- Run Test 5 (All Patterns Combined)
- Show the response in Swagger UI
- Open Output window in Visual Studio and show logs

### 3. **Explain the Flow**
```
Test 5 Flow:
1. Factory creates volunteer (VolunteerFactory)
   ? Observer notifies (registration event)
2. Observer updates skills
   ? 3 observers triggered (Logging, Statistics, Notification)
3. Decorator enriches profile
   ? Goes through Validation ? Logging ? Enrichment chain
```

### 4. **Show SOLID Principles**
- Point to interfaces (`IUserFactory`, `IVolunteerProfileService`, `IVolunteerObserver`)
- Show DI registration in `Program.cs`
- Explain how each pattern can be extended without modifying existing code

---

## Expected Logs Output

When running Test 5, you should see logs like:

```
[15:30:45] [VOLUNTEER EVENTS] Observer subscribed. Total observers: 3
[15:30:45] [VOLUNTEER EVENTS] Notifying 3 observers about volunteer registration: 123
[15:30:45] [VOLUNTEER LOG] New volunteer registered: 123 - combined@example.com
[15:30:45] [VOLUNTEER STATS] Total registrations: 1
[15:30:45] [VOLUNTEER NOTIFICATION] Sending welcome email to combined@example.com

[15:30:46] [VOLUNTEER EVENTS] Notifying 3 observers about skills update: 123
[15:30:46] [VOLUNTEER LOG] Skills updated for volunteer 123: C#, ASP.NET Core, Design Patterns
[15:30:46] [VOLUNTEER STATS] Total skill updates: 1
[15:30:46] [VOLUNTEER NOTIFICATION] Sending skills update confirmation to combined@example.com

[15:30:47] [VOLUNTEER PROFILE] Retrieving profile for volunteer 123
[15:30:47] [VOLUNTEER PROFILE] Validation passed for volunteer 123
[15:30:47] [VOLUNTEER PROFILE] Formatting summary for volunteer 123
[15:30:47] [VOLUNTEER PROFILE] Enriched summary created for volunteer 123
```

---

## Grading Checklist

? **I3 Criteria (1 bod):**
- [x] Minimum 3 design patterns implemented
- [x] From different categories (Creational, Structural, Behavioral)
- [x] Properly integrated into the application

? **I8 Criteria (1 bod):**
- [x] Each team member implemented patterns in their module
- [x] Volunteer module: Factory + Decorator + Observer
- [x] Patterns are separate from other modules (Admin, Organization)

? **Bonus Points:**
- [x] SOLID principles followed
- [x] Clean code and documentation
- [x] Working demo with Swagger
- [x] Comprehensive documentation (DESIGN_PATTERNS_VOLUNTEER_MODULE.md)

---

## Troubleshooting

### If Swagger doesn't show the new endpoints:
1. Rebuild the solution: `Ctrl+Shift+B`
2. Restart the application
3. Clear browser cache and reload Swagger page

### If observers don't trigger:
- Check that observers are registered in `Program.cs` (Line 59-63)
- Check that `VolunteerEventPublisher` is injected in `UserService`

### If decorator doesn't work:
- Check that `IVolunteerProfileService` is registered in DI (Line 46-57)
- Verify decorator chain order in `Program.cs`

---

## Summary

This demo shows:
1. **Factory Method** creates volunteers dynamically
2. **Decorator** enhances volunteer profiles with validation, logging, enrichment
3. **Observer** notifies multiple subscribers about volunteer events

All patterns are **volunteer-specific**, **SOLID-compliant**, and **production-ready**!
