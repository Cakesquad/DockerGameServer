using DockerGameServer.Models.Database;
using DockerGameServer.Repositories;
using System.ComponentModel.DataAnnotations;

namespace DockerGameServer.Services
{
	public class LoginModel
	{
		[Required(ErrorMessage = "Email is required")]
		[EmailAddress(ErrorMessage = "Invalid email format")]
		public string Email { get; set; }

		[Required(ErrorMessage = "Password is required")]
		public string Password { get; set; }
	}
	public class RegisterModel
	{
		[Required(ErrorMessage = "FullName is required")]
		public string FullName { get; set; }

		[Required(ErrorMessage = "Password is required")]
		[Length(6, 100, ErrorMessage = "Password must be between 6 and 100 characters")]
		public string Password { get; set; }

		[Required(ErrorMessage = "ConfirmPassword is required")]
		[Compare("Password", ErrorMessage = "Passwords do not match")]
		public string ConfirmPassword { get; set; }

		[Required(ErrorMessage = "Email is required")]
		[EmailAddress(ErrorMessage = "Invalid email format")]
		public string Email { get; set; }
	}
	public class UserService(UserRepository userRepository, EncryptionService encryptionService)
	{
		public async Task<bool> CreateAsync(RegisterModel model)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			if (await userRepository.IsEmailRegisteredAsync(encryptionService.CreateLookupHash(model.Email)))
				throw new InvalidOperationException("Email is already registered");

			var user = new User
			{
				PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
				EmailHash = encryptionService.CreateLookupHash(model.Email),
				Data = new UserInfo
				{
					FullName = model.FullName,
					Email = model.Email
				}
			};

			await userRepository.AddAsync(user);
			return true;
		}

		public async Task<bool> RemoveAsync(Guid id)
		{
			if (id == Guid.Empty)
				throw new ArgumentNullException(nameof(id));

			var user = await userRepository.GetByIdAsync(id);
			if (user == null)
				return false;

			await userRepository.DeleteAsync(user);
			return true;
		}

		public async Task<User> LoginAsync(LoginModel model)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			var user = await userRepository.GetByEmailHashAsync(encryptionService.CreateLookupHash(model.Email));
			if (user == null)
				throw new InvalidOperationException("Invalid email or password");

			if (!BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
				throw new InvalidOperationException("Invalid email or password");

			return user;
		}
	}
}