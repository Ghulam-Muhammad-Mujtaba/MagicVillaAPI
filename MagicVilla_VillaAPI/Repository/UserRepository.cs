using MagicVilla_VillaAPI.Data;
using MagicVilla_VillaAPI.Models;
using MagicVilla_VillaAPI.Models.Dto;
using MagicVilla_VillaAPI.Repository.IRepository;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MagicVilla_VillaAPI.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _db;
        private string secretKey;

        public UserRepository(ApplicationDbContext db, IConfiguration configuration)
        {
            _db = db;
            //Get Secret key from appsettings.json
            secretKey = configuration.GetValue<string>("ApiSettings:Secret");
        }
        public bool IsUniqueUser(string username)
        {
            var user = _db.LocalUsers.FirstOrDefault(x => x.UserName == username);
            if (user == null)
            {
                //No user is found
                return true;
            }

            //User exists
            return false;
        }

        public async Task<LoginResponseDTO> Login(LoginRequestDTO loginRequestDTO)
        {
            //Validate User name and password
            var user = _db.LocalUsers.FirstOrDefault(u => u.UserName.ToLower() == loginRequestDTO.UserName.ToLower()
           && u.Password == loginRequestDTO.Password);

            if (user == null)
            {
                return new LoginResponseDTO()
                {
                    Token = "",
                    User = null
                };
            }

            //if user was found generate JWT Token

            var tokenHandler = new JwtSecurityTokenHandler();

            //Convert secretkey into byte and store in key variable
            var key = Encoding.ASCII.GetBytes(secretKey);

            //Defining properties contain by Token
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                //Array of claims, you can add email also
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, user.Id.ToString()),
                    new Claim(ClaimTypes.Role, user.Role)
                }),

                //Expiry Date
                Expires = DateTime.UtcNow.AddDays(7),

                //Symmetric key and type of encryption algorithm
                SigningCredentials = new(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            //Generating JWT Token
            var token = tokenHandler.CreateToken(tokenDescriptor);

            //Creating return type
            LoginResponseDTO loginResponseDTO = new LoginResponseDTO()
            {
                //Serialized Token
                Token = tokenHandler.WriteToken(token),
                User = user
            };

            return loginResponseDTO;
        }

        public async Task<LocalUser> Register(RegisterationRequestDTO registerationRequestDTO)
        {
            LocalUser user = new()
            {
                UserName = registerationRequestDTO.UserName,
                Password = registerationRequestDTO.Password,
                Name = registerationRequestDTO.Name,
                Role = registerationRequestDTO.Role
            };

            _db.LocalUsers.Add(user);
            await _db.SaveChangesAsync();

            //Remove Password from object
            user.Password = "";
            return user;
        }
    }
}
