using WebApp.Models;

namespace WebApp.Patterns.Creational
{

    public interface IUserFactory
    {
        User CreateUser(string email, string firstName, string lastName, string phoneNumber);
        UserRole SupportedRole { get; }
    }


    public class OrganizationFactory : IUserFactory
    {
        public UserRole SupportedRole => UserRole.Organization;

        public User CreateUser(string email, string firstName, string lastName, string phoneNumber)
        {
            return new Organization
            {
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = phoneNumber,
                Role = UserRole.Organization,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                IsVerified = false,
                OrganizationName = string.Empty,
                Description = string.Empty,
                Address = string.Empty,
                City = string.Empty
            };
        }
    }


    public interface IUserFactoryProvider
    {

        IUserFactory GetFactory(UserRole role);
       
        User CreateUser(UserRole role, string email, string firstName, string lastName, string phoneNumber);
    }


    public class UserFactoryProvider : IUserFactoryProvider
    {
        private readonly Dictionary<UserRole, IUserFactory> _factories;

        public UserFactoryProvider(IEnumerable<IUserFactory> factories)
        {
            _factories = factories.ToDictionary(f => f.SupportedRole);
        }

        public IUserFactory GetFactory(UserRole role)
        {
            if (!_factories.TryGetValue(role, out var factory))
            {
                throw new ArgumentException($"No factory registered for role: {role}", nameof(role));
            }
            return factory;
        }

        public User CreateUser(UserRole role, string email, string firstName, string lastName, string phoneNumber)
        {
            var factory = GetFactory(role);
            return factory.CreateUser(email, firstName, lastName, phoneNumber);
        }
    }
}
