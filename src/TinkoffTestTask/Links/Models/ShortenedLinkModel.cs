using System;
using System.Collections.Generic;

namespace TinkoffTestTask.Links.Models
{
	public class ShortenedLinkModel
	{
		public ShortenedLinkModelId Id { get; set; }
		public string Key { get; set; }
		public string Value { get; set; }
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public IEnumerable<Follow> Follows { get; set; } = Array.Empty<Follow>();

		public class Follow
		{
			public string IP { get; set; }
		}

		public class ShortenedLinkModelId
		{
			public long UserId { get; set; }
			public long LinkId { get; set; }
		}
	}
}
