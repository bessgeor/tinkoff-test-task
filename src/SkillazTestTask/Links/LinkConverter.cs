using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SkillazTestTask.Links
{
	public static class LinkConverter
	{
		private static readonly char[] _alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ01234567890!$^*-_+".ToCharArray();
		private static readonly Dictionary<char, long> _alphabetInversion = _alphabet.Select( ( c, i ) => (c, i) ).ToDictionary( t => t.c, t => (long) t.i );
		private static readonly long _alphabetPower = _alphabet.Length; // long to avoid casts

		public static string GenerateKey( long @for )
		{
			StringBuilder sb = new StringBuilder( 4 );
			unchecked // optimize out overflow checking
			{
				do
				{
					sb.Append( _alphabet[ (int) ( @for % _alphabetPower ) ] ); /// produces reverted number in base <see cref="_alphabetPower"/>
					@for /= _alphabetPower;
				}
				while ( @for > 0 );
			}
			return sb.ToString();
		}

		public static long RegenerateId( string @from )
		{
			long value = 0;
			unchecked // optimize out overflow checking
			{
				for ( int i = from.Length - 1 /* recall revertion */; i > -1; i-- )
					value = value * _alphabetPower + _alphabetInversion[ from[ i ] ];
			}
			return value;
		}
	}
}
