using System;
using System.Collections.Generic;

namespace TinkoffTestTask.Links.Models
{
	public class ShortenedLinkModel
	{
		public long Id { get; set; }
		public string Key { get; set; }
		public string Value { get; set; }
		public IEnumerable<Follow> Follows { get; set; } = Array.Empty<Follow>();

		public class Follow
		{
			public string IP { get; set; }
		}
	}
}
