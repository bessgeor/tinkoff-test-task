using Microsoft.AspNetCore.Http;
using System;
using System.Threading;
using System.Threading.Tasks;
using TinkoffTestTask.Sequences;
using TinkoffTestTask.Utils;

namespace TinkoffTestTask.Auth
{
	public class AuthTokenProvider : IAuthTokenProvider
	{
		private const string _cookieName = "tinkoff-test-task-auth-cookie";
		private readonly Sequence _authorizedUsersSequence;
		private readonly DecBase68Converter _converter;

		public AuthTokenProvider( ISequenceProvider provider, DecBase68Converter converter )
		{
			_authorizedUsersSequence = provider.GetSequenceAsync( "authorizedUsersSequence" ).GetAwaiter().GetResult();
			_converter = converter;
		}

		public Task<string> GetTokenAsync( HttpContext context, bool createIfNotExists )
		{
			string current = context.Request.Cookies[ _cookieName ];
			if ( current == null && createIfNotExists )
				return GenerateAndSetCookie();
			return Task.FromResult( current );

			async Task<string> GenerateAndSetCookie()
			{
				long next;
				using ( CancellationTokenSource source = new CancellationTokenSource( TimeSpan.FromSeconds( 1.5 ) ) )
				{
					Sequence actual = await _authorizedUsersSequence.GetNextSequenceValue( source.Token ).ConfigureAwait( false );
					next = actual.Value;
				}
				string value = _converter.GenerateKey( next );
				context.Response.Cookies.Append( _cookieName, value );
				return value;
			}
		}
	}
}
