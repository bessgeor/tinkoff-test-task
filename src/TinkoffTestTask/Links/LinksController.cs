using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using TinkoffTestTask.Links.Models;
using TinkoffTestTask.Sequences;
using TinkoffTestTask.Utils;
using TinkoffTestTask.Auth;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static TinkoffTestTask.Links.Models.ShortenedLinkModel;

namespace TinkoffTestTask.Links
{
	public class LinksController : Controller
	{
		private static readonly InsertOneOptions _insertOptions = new InsertOneOptions() { BypassDocumentValidation = false };
		private static readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds( 1.5d );

		private readonly IMongoCollection<ShortenedLinkModel> _links;
		private readonly DecBase68Converter _converter;
		private readonly IAuthTokenProvider _authTokenProvider;
		private readonly ISequenceProvider _sequenceProvider;

		public LinksController( IMongoDatabase db, ISequenceProvider provider, DecBase68Converter converter, IAuthTokenProvider authTokenProvider )
		{
			_links = db.GetCollection<ShortenedLinkModel>( "ShortenedLinks" );
			_sequenceProvider = provider;
			_converter = converter;
			_authTokenProvider = authTokenProvider;
		}

		// should be PUT probably, but GET is much easer to test
		[HttpGet( "compress/{*url}" )]
		public Task<string> Compress( string url )
		{
			if ( !Uri.IsWellFormedUriString( url, UriKind.Absolute ) )
			{
				Response.StatusCode = 400; // bad request
				return Task.FromResult( "Error: url should be absolute" ); // don't allocate an async state machine if user provided invalid input
			}
			return CompressInternal();

			async Task<string> CompressInternal()
			{
				string userKey = await _authTokenProvider.GetTokenAsync( HttpContext, createIfNotExists: true ).ConfigureAwait( false );

				long newlyGeneratedLinkId;
				Sequence seq = await _sequenceProvider.GetSequenceAsync( "linksSequence_" + userKey ).ConfigureAwait( false );
				using ( CancellationTokenSource source = new CancellationTokenSource( _defaultTimeout ) )
				{
					seq = await seq.GetNextSequenceValue( source.Token ).ConfigureAwait( false );
					newlyGeneratedLinkId = seq.Value;
				}
				string key = String.Concat( userKey, "~", _converter.GenerateKey( newlyGeneratedLinkId ) );
				long userId = _converter.RegenerateId( userKey );
				ShortenedLinkModel model = new ShortenedLinkModel { Id = new ShortenedLinkModelId { LinkId = newlyGeneratedLinkId, UserId = userId }, Key = key, Value = url };
				
				using ( CancellationTokenSource source = new CancellationTokenSource( _defaultTimeout ) )
					await _links.InsertOneAsync( model, _insertOptions, source.Token ).ConfigureAwait( false );
				
				return model.Key;
			}
		}

		[HttpGet( "f/{userKey}~{linkKey}")]
		public Task Decompress( string userKey, string linkKey )
		{
			long userId = _converter.RegenerateId( userKey );
			long linkId = _converter.RegenerateId( linkKey );
			ShortenedLinkModelId id = new ShortenedLinkModelId { UserId = userId, LinkId = linkId };

			return DecompressInternal(); // possible (if regeneration throws) async state machine allocation avoidance

			async Task DecompressInternal()
			{
				string link;
				using ( CancellationTokenSource source = new CancellationTokenSource( _defaultTimeout ) )
					link = await _links
					.FindOneAndUpdateAsync
					(
						m => m.Id == id,
						new UpdateDefinitionBuilder<ShortenedLinkModel>()
							.AddToSet( m => m.Follows, new Follow { IP = HttpContext.Connection.RemoteIpAddress.ToString() } ),
						new FindOneAndUpdateOptions<ShortenedLinkModel, string>()
						{
							Projection = new ProjectionDefinitionBuilder<ShortenedLinkModel>().Expression( m => m.Value )
						},
						source.Token
					).ConfigureAwait( false );

				if ( link is null )
				{
					Response.StatusCode = 404;
					return;
				}

				Response.StatusCode = 302;
				Response.Headers.Add( "Location", link );
			}
		}

		[HttpGet( "list" )]
		public async Task<ShortenedLinkViewModel[]> List( int? from = null, int? count = null )
		{
			string userKey = await _authTokenProvider.GetTokenAsync( HttpContext, createIfNotExists: false ).ConfigureAwait( false );
			if ( userKey is null )
				return Array.Empty<ShortenedLinkViewModel>();

			long userId = _converter.RegenerateId( userKey );

			using ( CancellationTokenSource source = new CancellationTokenSource( _defaultTimeout ) )
			using ( IAsyncCursor<ShortenedLinkViewModel> cursor = await _links
				.FindAsync
				(
					Builders<ShortenedLinkModel>.Filter.Where( m => m.Id.UserId == userId ),
					new FindOptions<ShortenedLinkModel, ShortenedLinkViewModel>
					{
						Skip = from,
						Limit = count,
						BatchSize = count,
						Projection = Builders<ShortenedLinkModel>.Projection.Expression
						(
							m => new ShortenedLinkViewModel
							{
								Key = m.Key,
								Value = m.Value,
								CreatedAt = m.CreatedAt,
								FollowCount = m.Follows.Count()
							}
						)
					},
					source.Token
				)
				.ConfigureAwait( false ) )
			{
				using ( CancellationTokenSource sourceInner = new CancellationTokenSource( _defaultTimeout ) )
					await cursor.MoveNextAsync( sourceInner.Token ).ConfigureAwait( false );
				return cursor.Current.ToArray();
			}
		}
	}
}
