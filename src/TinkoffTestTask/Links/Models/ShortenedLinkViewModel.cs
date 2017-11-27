using System;

namespace TinkoffTestTask.Links.Models
{
	public class ShortenedLinkViewModel
	{
		public string Key { get; set; }
		public string Value { get; set; }
		public DateTime CreatedAt { get; set; }
		public long FollowCount { get; set; }
	}
}
