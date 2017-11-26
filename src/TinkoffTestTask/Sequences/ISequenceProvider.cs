using System.Threading.Tasks;

namespace TinkoffTestTask.Sequences
{
	public interface ISequenceProvider
    {
		Task<Sequence> GetSequenceAsync( string name );
    }
}
