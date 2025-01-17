﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Lampac.Engine;
using Lampac.Engine.CORE;
using System.Web;
using Microsoft.Extensions.Caching.Memory;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Lampac.Controllers.LITE
{
    public class AniMedia : BaseController
    {
        [HttpGet]
        [Route("lite/animedia")]
        async public Task<ActionResult> Index(string title, string code, int entry_id, int s = -1)
        {
            if (!AppInit.conf.AniMedia.enable || string.IsNullOrWhiteSpace(title))
                return Content(string.Empty);

            bool firstjson = true;
            string html = "<div class=\"videos__line\">";

            if (string.IsNullOrWhiteSpace(code))
            {
                #region Поиск
                string memkey = $"animedia:search:{title}";
                if (!memoryCache.TryGetValue(memkey, out List<(string title, string code)> catalog))
                {
                    string search = await HttpClient.Get($"{AppInit.conf.AniMedia.host}/ajax/search_result_search_page_2/P0?limit=12&keywords={HttpUtility.UrlEncode(title)}&orderby_sort=entry_date|desc", timeoutSeconds: 8, useproxy: AppInit.conf.AniMedia.useproxy);
                    if (search == null)
                        return Content(string.Empty);

                    catalog = new List<(string title, string url)>();

                    foreach (string row in search.Split("<div class=\"ads-list__item\">").Skip(1))
                    {
                        var g = Regex.Match(row, "href=\"/anime/([^\"]+)\"[^>]+ class=\"h3 ads-list__item__title\">([^<]+)</a>").Groups;

                        if (!string.IsNullOrWhiteSpace(g[1].Value) && !string.IsNullOrWhiteSpace(g[2].Value))
                            catalog.Add((g[2].Value, g[1].Value));
                    }

                    if (catalog.Count == 0)
                        return Content(string.Empty);

                    memoryCache.Set(memkey, catalog, DateTime.Now.AddMinutes(AppInit.conf.multiaccess ? 40 : 10));
                }

                if (catalog.Count == 1)
                    return LocalRedirect($"/lite/animedia?title={HttpUtility.UrlEncode(title)}&code={catalog[0].code}");

                foreach (var res in catalog)
                {
                    string link = $"{AppInit.Host(HttpContext)}/lite/animedia?title={HttpUtility.UrlEncode(title)}&code={res.code}";

                    html += "<div class=\"videos__item videos__season selector " + (firstjson ? "focused" : "") + "\" data-json='{\"method\":\"link\",\"url\":\"" + link + "\",\"similar\":true}'><div class=\"videos__season-layers\"></div><div class=\"videos__item-imgbox videos__season-imgbox\"><div class=\"videos__item-title videos__season-title\">" + res.title + "</div></div></div>";
                    firstjson = false;
                }
                #endregion
            }
            else 
            {
                if (s == -1)
                {
                    #region Сезоны
                    string memKey = $"animedia:seasons:{code}";
                    if (!memoryCache.TryGetValue(memKey, out List<(string name, string uri)> links))
                    {
                        string news = await HttpClient.Get($"{AppInit.conf.AniMedia.host}/anime/{code}/1/1", timeoutSeconds: 8, useproxy: AppInit.conf.AniMedia.useproxy);
                        if (news == null)
                            return Content(string.Empty);

                        string entryid = Regex.Match(news, "name=\"entry_id\" value=\"([0-9]+)\"").Groups[1].Value;
                        if (string.IsNullOrWhiteSpace(entryid))
                            return Content(string.Empty);

                        links = new List<(string, string)>();

                        var match = Regex.Match(news, $"<a href=\"/anime/{code}/([0-9]+)/1\" class=\"item\">([^<]+)</a>");
                        while (match.Success)
                        {
                            if (!string.IsNullOrWhiteSpace(match.Groups[1].Value) && !string.IsNullOrWhiteSpace(match.Groups[2].Value))
                                links.Add((match.Groups[2].Value.ToLower(), $"{AppInit.Host(HttpContext)}/lite/animedia?title={HttpUtility.UrlEncode(title)}&code={code}&s={match.Groups[1].Value}&entry_id={entryid}"));

                            match = match.NextMatch();
                        }

                        if (links.Count == 0)
                            return Content(string.Empty);

                        memoryCache.Set(memKey, links, DateTime.Now.AddMinutes(AppInit.conf.multiaccess ? 30 : 10));
                    }

                    foreach (var l in links)
                    {
                        html += "<div class=\"videos__item videos__season selector " + (firstjson ? "focused" : "") + "\" data-json='{\"method\":\"link\",\"url\":\"" + l.uri + "\"}'><div class=\"videos__season-layers\"></div><div class=\"videos__item-imgbox videos__season-imgbox\"><div class=\"videos__item-title videos__season-title\">" + l.name + "</div></div></div>";
                        firstjson = false;
                    }
                    #endregion
                }
                else
                {
                    #region Серии
                    string memKey = $"animedia:playlist:{entry_id}:{s}";
                    if (!memoryCache.TryGetValue(memKey, out List<(string name, string uri)> links))
                    {
                        var playlist = await HttpClient.Get<JArray>($"{AppInit.conf.AniMedia.host}/embeds/playlist-j.txt/{entry_id}/{s}", timeoutSeconds: 8, useproxy: AppInit.conf.AniMedia.useproxy);
                        if (playlist == null || playlist.Count == 0)
                            return Content(string.Empty);

                        links = new List<(string name, string uri)>();

                        foreach (var pl in playlist)
                        {
                            string name = pl.Value<string>("title");
                            string file = pl.Value<string>("file");
                            if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(file))
                                links.Add((name, file));
                        }

                        if (links.Count == 0)
                            return Content(string.Empty);

                        memoryCache.Set(memKey, links, DateTime.Now.AddMinutes(AppInit.conf.multiaccess ? 30 : 10));
                    }

                    foreach (var l in links)
                    {
                        string link = AppInit.conf.AniMedia.streamproxy ? $"{AppInit.Host(HttpContext)}/proxy/{l.uri}" : l.uri;
                        html += "<div class=\"videos__item videos__movie selector " + (firstjson ? "focused" : "") + "\" media=\"\" s=\"" + s + "\" e=\"" + Regex.Match(l.name, "([0-9]+)$").Groups[1].Value + "\" data-json='{\"method\":\"play\",\"url\":\"" + link + "\",\"title\":\"" + $"{title} ({l.name.ToLower()})" + "\"}'><div class=\"videos__item-imgbox videos__movie-imgbox\"></div><div class=\"videos__item-title\">" + l.name + "</div></div>";
                        firstjson = true;
                    }
                    #endregion
                }
            }

            return Content(html + "</div>", "text/html; charset=utf-8");
        }
    }
}
