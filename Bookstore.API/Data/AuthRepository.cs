using System;
using System.Linq;
using System.Threading.Tasks;
using Bookstore.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.API.Data
{
    public class AuthRepository : IAuthRepository
    {
        private readonly DataContext context;
        
        public AuthRepository(DataContext context)
        {
            this.context = context;
        }

        public async Task<User> Login(string username, string password)
        {
            var user = await context.Users.FirstOrDefaultAsync(x => x.Username.ToLower() == username.ToLower());

            if (user == null)
            {
                return null;
            }

            if (!this.VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
            {
                return null;
            }

            return user;
        }

        private bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
        {
            using (var hmac = new System.Security.Cryptography.HMACSHA512(passwordSalt))
            {
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));

                for (int i = 0; i < computedHash.Length; i++)
                {
                    if (computedHash[i] != passwordHash[i])
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public async Task<User> Register(User user, string password)
        {
            byte[] passwordHash, passwordSalt;

            this.CreatePasswordHash(password, out passwordHash, out passwordSalt);

            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;

            await context.Users.AddAsync(user);

            await context.SaveChangesAsync();

            return user;
        }

        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            if (password == null)
            {
                throw new ArgumentNullException("password");
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Value cannot be empty or whitespace only string.", "password");
            }

            using (var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }    
        }

        public async Task<bool> UserExists(string username)
        {
            if (await context.Users.AnyAsync(x => x.Username == username))
            {
                return true;
            }

            return false;
        }
    }
}