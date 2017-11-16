using MongoDB.Driver;

namespace SkillazTestTask.Utils.MongoRelatedExtensions
{
	public static class MongoWriteExceptioExtensions
	{
		public static bool MeansDuplicateKey( this MongoWriteException e )
			=> e.Message.Contains( "E11000 duplicate key error" );
	}
}
