using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace TinkoffTestTask.Auth
{
	public interface IAuthTokenProvider
	{
		Task<string> GetTokenAsync( HttpContext context, bool createIfNotExists );
	}
}
