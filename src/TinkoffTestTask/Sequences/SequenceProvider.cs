using MongoDB.Driver;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace TinkoffTestTask.Sequences
{
	public class SequenceProvider : ISequenceProvider
    {
		private static ConcurrentDictionary<string, Sequence> _sequenceCache = new ConcurrentDictionary<string, Sequence>();

		private readonly IMongoDatabase _db;

		public SequenceProvider( IMongoDatabase db )
			=> _db = db;

		public Task<Sequence> GetSequenceAsync( string name )
			=> _sequenceCache.TryGetValue( name, out Sequence seq ) // is cheaper than db query and async state machine allocation
			? Task.FromResult( seq )
			: CreateSequence( name );

		private async Task<Sequence> CreateSequence( string name )
		{
			Sequence seq = _sequenceCache.GetOrAdd( name, nameInFactory => new Sequence { Id = nameInFactory } );
			await seq.EnsureCreated( _db ).ConfigureAwait( false );
			return seq;
		}
    }
}
