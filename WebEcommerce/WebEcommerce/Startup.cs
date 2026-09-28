using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(WebEcommerce.Startup))]
namespace WebEcommerce
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
