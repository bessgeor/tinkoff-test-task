using MongoDB.Driver;
using TinkoffTestTask.Utils.MongoRelatedExtensions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TinkoffTestTask.Sequences
{
	public class Sequence
    {
		private const string _collectionName = "sequences";
		private readonly IMongoDatabase _db;

		public Sequence( IMongoDatabase db )
			=> _db = db;

		public string Id { get; set; }
		public long Value { get; set; } = -1;

		public async Task EnsureCreated()
		{
			try
			{
				using ( CancellationTokenSource source = new CancellationTokenSource( TimeSpan.FromSeconds( 3.0d ) ) )
					await _db.GetCollection<Sequence>( _collectionName ).InsertOneAsync( this, new InsertOneOptions { BypassDocumentValidation = false }, source.Token ).ConfigureAwait( false );
			}
			catch( MongoWriteException e ) when ( e.MeansDuplicateKey() )
			{
				// is expected here if it is not the first run ever
			}
		}

		public Task<Sequence> GetNextSequenceValue( CancellationToken token )
			=> _db.GetCollection<Sequence>( _collectionName )
			.FindOneAndUpdateAsync<Sequence>
			(
				s => s.Id == Id,
				new UpdateDefinitionBuilder<Sequence>().Inc( s => s.Value, 1 ),
				new FindOneAndUpdateOptions<Sequence>() { ReturnDocument = ReturnDocument.After },
				token
			);
	}
}
