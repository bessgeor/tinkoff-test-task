using System.Text;

namespace SkillazTestTask.Links
{
	public static class LinkConverter
	{
		private static readonly char[] _alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ01234567890!$^*-_+".ToCharArray();
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
	}
}
