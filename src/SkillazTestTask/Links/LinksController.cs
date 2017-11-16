using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SkillazTestTask.Links.Models;
using SkillazTestTask.Sequences;
using SkillazTestTask.Utils.MongoRelatedExtensions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SkillazTestTask.Links
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
	}
}
