using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using TinkoffTestTask.Links.Models;
using TinkoffTestTask.Sequences;
using TinkoffTestTask.Utils.MongoRelatedExtensions;
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
		private readonly Sequence _linkIdSequence;

		public LinksController( IMongoDatabase db, Sequence linksSequence )
			=> (_links, _linkIdSequence) = (db.GetCollection<ShortenedLinkModel>( "ShortenedLinks" ), linksSequence);

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
				long newlyGeneratedId;
				using ( CancellationTokenSource source = new CancellationTokenSource( _defaultTimeout ) )
				{
					Sequence seq = await _linkIdSequence.GetNextSequenceValue( _links.Database, source.Token ).ConfigureAwait( false );
					newlyGeneratedId = seq.Value;
				}
				string key = LinkConverter.GenerateKey( newlyGeneratedId );
				ShortenedLinkModel model = new ShortenedLinkModel { Id = newlyGeneratedId, Key = key, Value = url };
				
				using ( CancellationTokenSource source = new CancellationTokenSource( _defaultTimeout ) )
					await _links.InsertOneAsync( model, _insertOptions, source.Token ).ConfigureAwait( false );
				
				return model.Key;
			}
		}

		[HttpGet( "f/{key}")]
		public Task Decompress( string key )
		{
			long id = LinkConverter.RegenerateId( key );

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
		public async Task<ShortenedLinkModel[]> List( int from, int count )
		{
			using ( CancellationTokenSource source = new CancellationTokenSource( _defaultTimeout ) )
			using ( IAsyncCursor<ShortenedLinkModel> cursor = await _links
				.FindAsync( m => true,
				new FindOptions<ShortenedLinkModel>()
				{ Skip = from, Limit = count, BatchSize = count },
				source.Token ).ConfigureAwait( false ) )
			{
				using ( CancellationTokenSource sourceInner = new CancellationTokenSource( _defaultTimeout ) )
					await cursor.MoveNextAsync( sourceInner.Token ).ConfigureAwait( false );
				return cursor.Current.ToArray();
			}
		}
	}
}
