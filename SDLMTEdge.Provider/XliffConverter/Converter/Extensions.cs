﻿using Sdl.LanguagePlatform.Core;
using System.Diagnostics;
using System.Text;

namespace Sdl.Community.MTEdge.Provider.XliffConverter.Converter
{
	public static  class Extensions
	{
		public static string ToXliffString(this Segment segment)
		{
			// No matter what, always encode < to &lt; so xliff doesn't recognize this as part of a tag
			if (!segment.HasTags)
            {
                return segment.ToPlain()
							  .Replace("<", "&lt;");
            }

            var result = new StringBuilder();
            for (int i = 0; i < segment.Elements.Count; i++)
			{
                SegmentElement element = segment.Elements[i];
                if (element is Text txt)
				{
					result.Append(txt.Value.Replace("<", "&lt;"));
					continue;
				}
				else if (element is not Tag)
				{
					continue;
				}

				var tag = element as Tag;
				var tagString = tag.ToString().Replace("<", "&lt;");
                switch (tag.Type)
                {
                    case TagType.Start:
                        result.Append($"<bpt id=\"{tag.TagID}\">{tagString}</bpt>");
                        break;
                    case TagType.End:
                        result.Append($"<ept id=\"{tag.TagID}\">{tagString}</ept>");
                        break;
                    case TagType.UnmatchedStart:
                        break;
                    case TagType.UnmatchedEnd:
                        result.Append($"<it id=\"{tag.TagID}\">{tagString}</it>");
                        break;
                    case TagType.Standalone:
                        break;
                    case TagType.TextPlaceholder:
                        result.Append($"<x id=\"{tag.TagID}\">{tagString}</x>");
                        break;
                    case TagType.LockedContent:
                        result.Append($"<x id=\"{tag.TagID}\">{tagString.Replace("/>", @" locked=""true""/>")}</x>");
                        break;
                    case TagType.Undefined:
                        Debug.Assert(false, "Unexpected tag type");
                        break;
                    default:
                        Debug.Assert(false, "Unexpected tag type");
                        break;
                }
            }

            return result.ToString();
        }
    }
}