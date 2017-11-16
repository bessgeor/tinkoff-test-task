using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SkillazTestTask.Links.Models;

namespace SkillazTestTask.Links
{
	public class LinksController : Controller
	{
		private readonly IMongoCollection<ShortenedLinkModel> _links;
		private readonly Sequence _linkIdSequence;

		public LinksController( IMongoDatabase db, Sequence linksSequence )
			=> (_links, _linkIdSequence) = (db.GetCollection<ShortenedLinkModel>( "ShortenedLinks" ), linksSequence);
	}
}
