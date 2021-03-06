﻿using Quran.Core.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SQLite.Net;
using Windows.Storage;
using Microsoft.ApplicationInsights.DataContracts;

namespace Quran.Core.Data
{
    public enum BoomarkSortOrder
    {
        DateAdded = 0,
        Location = 1,
        Alphabetical = 2
    }

    public class BookmarksDatabaseHandler : BaseDatabaseHandler
    {
        public static string DB_NAME = "bookmarks.db";
        private static HashSet<int> _pageCache;

        public BookmarksDatabaseHandler()
            : base(ApplicationData.Current.RoamingFolder.Path, DB_NAME)
        { }

        protected override SQLiteConnection CreateDatabase(string path)
        {
            var newDb = base.CreateDatabase(path);
            newDb.CreateTable<Bookmarks>();
            newDb.CreateTable<Tags>();
            newDb.CreateTable<BookmarkTags>();
            return newDb;
        }

        public static HashSet<int> PageCache
        {
            get
            {
                if (_pageCache == null)
                {
                    _pageCache = new HashSet<int>();
                    using (var adapter = new BookmarksDatabaseHandler())
                    {
                        var bookmarkedPages = adapter.GetBookmarks(false, BoomarkSortOrder.Location).GroupBy(b => b.Page);
                        foreach (var page in bookmarkedPages)
                        {
                            _pageCache.Add(page.Key);
                        }
                    }
                }
                return _pageCache;
            }
        }

        public static bool IsPageBookmarked(int page)
        {
            return PageCache.Contains(page);
        }

        public List<Bookmarks> GetBookmarks(bool loadTags, BoomarkSortOrder sortOrder)
        {
            var bookmarks = dbConnection.Table<Bookmarks>();
            switch (sortOrder)
            {
                case BoomarkSortOrder.Location:
                    bookmarks = bookmarks.OrderBy(b => b.Page).OrderBy(b => b.Surah).OrderBy(b => b.Ayah);
                    break;
                default:
                    bookmarks = bookmarks.OrderByDescending(b => b.AddedDate);
                    break;
            }
            var result = bookmarks.ToList();
            if (loadTags)
            {
                var tags = GetTags().ToDictionary(t=>t.Id);
                var bookmarkTags = GetBookmarkTags();
                foreach (var bt in bookmarkTags)
                {
                    var bookmark = result.FirstOrDefault(b => b.Id == bt.BookmarkId);
                    var tag = tags[bt.TagId];
                    if (bookmark != null)
                    {
                        if (bookmark.Tags == null)
                            bookmark.Tags = new List<Tags>();
                        bookmark.Tags.Add(new Tags {Id = bt.TagId, Name = tag.Name});
                    }
                }
            }
            return result;
        }

        public List<int> GetBookmarkTagIds(int bookmarkId)
        {
            return dbConnection.Table<BookmarkTags>().Where(bt => bt.BookmarkId == bookmarkId).OrderBy(bt => bt.TagId).Select(bt => bt.TagId).ToList();
        }

        public List<BookmarkTags> GetBookmarkTags(int? bookmarkId = null)
        {
            var query = dbConnection.Table<BookmarkTags>();
            if (bookmarkId != null)
            {
                query = query.Where(bt => bt.BookmarkId == bookmarkId);
            }
            return query.ToList();
        }

        public bool TogglePageBookmark(int page)
        {
            var bookmarks = dbConnection.Table<Bookmarks>().Where(bt => bt.Page == page).Select(b => b.Id).ToList();
            if (bookmarks.Any())
            {
                bookmarks.ForEach(b => RemoveBookmark(b));
                return false;
            }
            else
            {
                AddBookmark(page);
                return true;
            }
        }

        public int GetBookmarkId(int? surah, int? ayah, int page)
        {
            var bookmarks = dbConnection.Table<Bookmarks>().Where(b => b.Page == page);
            if (surah != null)
            {
                bookmarks = bookmarks.Where(b => b.Surah == surah.Value);
            }
            if (ayah != null)
            {
                bookmarks = bookmarks.Where(b => b.Ayah == ayah.Value);
            }
            var results = bookmarks.ToList();
            if (results.Count > 0)
            {
                return results[0].Id;
            }
            return -1;
        }

        public bool IsTagged(int bookmarkId)
        {
            return dbConnection.Table<BookmarkTags>().Where(bt => bt.BookmarkId == bookmarkId).Count() > 0;
        }

        public int AddBookmark(int page)
        {
            return AddBookmark(null, null, page);
        }

        public int AddBookmarkIfNotExists(int? surah, int? ayah, int page)
        {
            int bookmarkId = GetBookmarkId(surah, ayah, page);
            if (bookmarkId < 0)
            {
                bookmarkId = AddBookmark(surah, ayah, page);
            }
            return bookmarkId;
        }

        public int AddBookmark(int? surah, int? ayah, int page)
        {
            var bookmark = new Bookmarks { Ayah = ayah, Surah = surah, Page = page };
            dbConnection.Insert(bookmark);
            PageCache.Add(page);
            return bookmark.Id;
        }

        public bool RemoveBookmark(int bookmarkId)
        {
            var bookmark = dbConnection.Table<Bookmarks>().Where(b => b.Id == bookmarkId).FirstOrDefault();
            ClearBookmarkTags(bookmarkId);
            var success = dbConnection.Delete(new Bookmarks { Id = bookmarkId }) == 1;
            if (success)
            {
                if (GetBookmarkId(null, null, bookmark.Page) < 0)
                {
                    PageCache.Remove(bookmark.Page);
                }
            }
            return success;
        }

        public List<Tags> GetTags()
        {
            return GetTags(BoomarkSortOrder.Alphabetical);
        }

        public List<Tags> GetTags(BoomarkSortOrder sortOrder)
        {
            var tags = dbConnection.Table<Tags>();
            switch (sortOrder)
            {
                case BoomarkSortOrder.Location:
                    tags = tags.OrderBy(t => t.Name);
                    break;
                default:
                    tags = tags.OrderByDescending(t => t.AddedDate);
                    break;
            }
            return tags.ToList();
        }

        public int AddTag(string name)
        {
            var existingTag = dbConnection.Table<Tags>().FirstOrDefault(t => t.Name.ToLower() == name.ToLower());

            if (existingTag == null)
            {
                Tags tag = new Tags {Name = name};
                return dbConnection.Insert(tag);
            }
            else
            {
                return existingTag.Id;
            }
        }

        public bool UpdateTag(int id, string newName)
        {
            Tags tag = new Tags { Id = id, Name = newName };

            return dbConnection.Update(tag) == 1;
        }

        public bool RemoveTag(int tagId)
        {
            bool removed = dbConnection.Delete(new Tags { Id = tagId }) == 1;
            if (removed)
            {
                dbConnection.Execute("delete from \"bookmark_tag\" where \"tag_id\" = ?", tagId);
            }

            return removed;
        }

        public int GetBookmarkTagId(int bookmarkId, int tagId)
        {
            var result = dbConnection.Table<BookmarkTags>().Where(bt => bt.BookmarkId == bookmarkId && bt.TagId == tagId).Select(bt => bt.Id);

            if (result.Count() == 0)
                return -1;
            else
                return result.First();
        }

        public void TagBookmarks(int[] bookmarkIds, List<Tags> tags)
        {
            dbConnection.BeginTransaction();
            try
            {
                foreach (var t in tags)
                {
                    if (t.Id < 0)
                        continue;

                    if (t.Checked)
                    {
                        for (int i = 0; i < bookmarkIds.Length; i++)
                        {
                            var btId = GetBookmarkTagId(bookmarkIds[i], t.Id);
                            if (btId == -1)
                            {
                                dbConnection.Insert(new BookmarkTags { BookmarkId = bookmarkIds[i], TagId = t.Id });
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < bookmarkIds.Length; i++)
                        {
                            var btId = GetBookmarkTagId(bookmarkIds[i], t.Id);
                            if (btId != -1)
                            {
                                dbConnection.Delete(new BookmarkTags { Id = btId });
                            }
                        }
                    }
                }
                dbConnection.Commit();
            }
            catch (Exception e)
            {
                telemetry.TrackException(e, new Dictionary<string, string> {{ "Scenario", "TagBookmarks" } });
                Debug.WriteLine("exception in tagBookmark: " + e.Message);
                dbConnection.Rollback();
            }
        }

        public void TagBookmark(int bookmarkId, List<Tags> tags)
        {
            TagBookmarks(new int[] { bookmarkId }, tags);
        }

        public void TagBookmark(int bookmarkId, int tagId)
        {
            var tag = dbConnection.Table<Tags>().Where(t => t.Id == tagId).FirstOrDefault();
            if (tag != null)
            {
                tag.Checked = true;
                var list = new List<Tags>();
                list.Add(tag);
                TagBookmarks(new int[] { bookmarkId }, list);
            }
        }

        public void UntagBookmark(int bookmarkId, int tagId)
        {
            dbConnection.Execute("delete \"bookmark_tag\" where \"bookmark_id\" = ? and \"tag_id\" = ?", bookmarkId, tagId);
        }

        public void ClearBookmarkTags(int bookmarkId)
        {
            dbConnection.Execute("delete from \"bookmark_tag\" where \"bookmark_id\" = ?", bookmarkId);
        }
    }
}
