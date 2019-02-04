﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PointOfInterestSkill.Models;

namespace PointOfInterestSkill.ServiceClients
{
    public sealed class AzureMapsGeoSpatialService : IGeoSpatialService
    {
        private static readonly string FindByFuzzyQueryApiUrl = $"https://atlas.microsoft.com/search/fuzzy/json?api-version=1.0&limit=3&lat={{0}}&lon={{1}}&query={{2}}&countryset={{3}}";
        private static readonly string FindByQueryApiUrl = $"https://atlas.microsoft.com/search/address/json?api-version=1.0&limit=3&query=";
        private static readonly string FindByPointUrl = $"https://atlas.microsoft.com/search/address/reverse/json?api-version=1.0&query={{0}},{{1}}";
        private static readonly string FindNearbyUrl = $"https://atlas.microsoft.com/search/nearby/json?api-version=1.0&limit=3&lat={{0}}&lon={{1}}";
        private static readonly string ImageUrlByPoint = $"https://atlas.microsoft.com/map/static/png?api-version=1.0&layer=basic&style=main&zoom={{2}}&center={{1}},{{0}}&width=512&height=512";
        private static readonly string GetRouteDirections = $"https://atlas.microsoft.com/route/directions/json?&api-version=1.0&query={{0}}";
        private static readonly string GetRouteDirectionsWithRouteType = $"https://atlas.microsoft.com/route/directions/json?&api-version=1.0&query={{0}}&&routeType={{1}}";
        private static string apiKey;
        private static string userLocale;
        private static HttpClient httpClient;

        public async Task<IGeoSpatialService> InitClientAsync(string clientId, string clientSecret, string locale = "en", HttpClient client = null)
        {
            throw new NotImplementedException();
        }

        public async Task<IGeoSpatialService> InitKeyAsync(string key, string locale = "en", HttpClient client = null)
        {
            try
            {
                apiKey = key;
                userLocale = locale;

                if (client == null)
                {
                    httpClient = ServiceHelper.GetHttpClient();
                }
                else
                {
                    httpClient = client;
                }
            }
            catch (Exception ex)
            {
            }

            return this;
        }

        /// <summary>
        /// Get points of interest weighted by coordinates and using a free for search query.
        /// </summary>
        /// <param name="latitude">The current latitude.</param>
        /// <param name="longitude">The current longitude.</param>
        /// <param name="query">The search query.</param>
        /// <param name="country">The user's country.</param>
        /// <returns>List of PointOfInterestModels.</returns>
        public async Task<List<PointOfInterestModel>> GetPointOfInterestByQueryAsync(double latitude, double longitude, string query, string country)
        {
            if (string.IsNullOrEmpty(query))
            {
                throw new ArgumentNullException(nameof(query));
            }

            if (string.IsNullOrEmpty(country))
            {
                throw new ArgumentNullException(nameof(country));
            }

            return await GetPointsOfInterestAsync(string.Format(CultureInfo.InvariantCulture, FindByFuzzyQueryApiUrl, latitude, longitude, query, country));
        }

        /// <summary>
        /// Get coordinates from a street address.
        /// </summary>
        /// <param name="address">The search address.</param>
        /// <returns>List of PointOfInterestModels.</returns>
        public async Task<List<PointOfInterestModel>> GetPointOfInterestByAddressAsync(string address)
        {
            if (string.IsNullOrEmpty(address))
            {
                throw new ArgumentNullException(nameof(address));
            }

            return await GetPointsOfInterestAsync(FindByQueryApiUrl + Uri.EscapeDataString(address));
        }

        /// <summary>
        /// Get a street address from coordinates.
        /// </summary>
        /// <param name="latitude">The current latitude.</param>
        /// <param name="longitude">The current longitude.</param>
        /// <returns>List of PointOfInterestModels.</returns>
        public async Task<List<PointOfInterestModel>> GetPointOfInterestByCoordinatesAsync(double latitude, double longitude)
        {
        return await GetPointsOfInterestAsync(
            string.Format(CultureInfo.InvariantCulture, FindByPointUrl, latitude, longitude));
        }

        /// <summary>
        /// Get Point of Interest results around a specific location.
        /// </summary>
        /// <param name="latitude">The current latitude.</param>
        /// <param name="longitude">The current longitude.</param>
        /// <returns>List of PointOfInterestModels.</returns>
        public async Task<List<PointOfInterestModel>> GetNearbyPointsOfInterestAsync(double latitude, double longitude)
        {
            return await GetPointsOfInterestAsync(
                string.Format(CultureInfo.InvariantCulture, FindNearbyUrl, latitude, longitude));
        }

        /// <summary>
        /// Get a static map image URL of the Point of Interest and returns PointOfInterestModel.
        /// </summary>
        /// <param name="pointOfInterest">The point of interest model.</param>
        /// <returns>PointOfInterestModel.</returns>
        public async Task<PointOfInterestModel> GetPointOfInterestDetailsAsync(PointOfInterestModel pointOfInterest)
        {
            int zoom = 15;

            string imageUrl = string.Format(
                CultureInfo.InvariantCulture,
                ImageUrlByPoint,
                pointOfInterest?.Geolocation?.Latitude,
                pointOfInterest?.Geolocation?.Longitude,
                zoom) + "&subscription-key=" + apiKey;

            pointOfInterest.ImageUrl = imageUrl;

            return pointOfInterest;
        }

        /// <summary>
        /// Get Azure Maps route based on available parameters.
        /// </summary>
        /// <param name="currentLatitude">The current latitude.</param>
        /// <param name="currentLongitude">The current longitude.</param>
        /// <param name="destinationLatitude">The destination latitude.</param>
        /// <param name="destinationLongitude">The destination longitude.</param>
        /// <param name="routeType">The (optional) route type.</param>
        /// <returns>RouteDirections.</returns>
        public async Task<RouteDirections> GetRouteDirectionsAsync(double currentLatitude, double currentLongitude, double destinationLatitude, double destinationLongitude, string routeType = null)
        {
            if (string.IsNullOrEmpty(routeType))
            {
                return await GetRouteDirectionsAsync(string.Format(CultureInfo.InvariantCulture, GetRouteDirections, currentLatitude + "," + currentLongitude + ":" + destinationLatitude + "," + destinationLongitude) + "&subscription-key=" + apiKey);
            }
            else
            {
                return await GetRouteDirectionsAsync(string.Format(CultureInfo.InvariantCulture, GetRouteDirectionsWithRouteType, currentLatitude + "," + currentLongitude + ":" + destinationLatitude + "," + destinationLongitude, routeType) + "&subscription-key=" + apiKey);
            }
        }

        /// <summary>
        /// Get route directions response from Azure Maps.
        /// </summary>
        /// <returns>RouteDirections.</returns>
        private async Task<RouteDirections> GetRouteDirectionsAsync(string url)
        {
            var response = await httpClient.GetStringAsync(url);

            var apiResponse = JsonConvert.DeserializeObject<RouteDirections>(response);

            return apiResponse;
        }

        /// <summary>
        /// Get search reuslts response from Azure Maps and convert to point of interest list.
        /// </summary>
        /// <returns>List of PointOfInterestModels.</returns>
        private async Task<List<PointOfInterestModel>> GetPointsOfInterestAsync(string url)
        {
            url = url + $"&language={userLocale}&subscription-key={apiKey}";

            var response = await httpClient.GetStringAsync(url);

            var apiResponse = JsonConvert.DeserializeObject<SearchResultSet>(response);

            var pointOfInterestList = new List<PointOfInterestModel>();

            if (apiResponse != null && apiResponse.Results != null)
            {
                foreach (var searchResult in apiResponse.Results)
                {
                    var newPointOfInterest = new PointOfInterestModel(searchResult);
                    pointOfInterestList.Add(newPointOfInterest);
                }
            }

            return pointOfInterestList;
        }
    }
}