using <%=assemblyName%>.Contract;

namespace <%=assemblyName%>.Service.Model
{
    public class LocalSignupOptions : ISignupServiceOptions
    {
        public string Username { get; set; }
        public bool Enabled { get; set; }
        public string DisplayName { get; set; }
        public string Password { get; set; }
        public bool Verified { get; set; }
        public string[] Roles { get; set; }
    }
}