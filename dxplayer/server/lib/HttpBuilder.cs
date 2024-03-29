﻿using io.github.toyota32k.server.model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace io.github.toyota32k.server {
    class HttpBuilder {
        public static IHttpResponse InternalServerError() {
            // string content = File.ReadAllText("Resources/Pages/500.html"); 

            return new TextHttpResponse() {
                ReasonPhrase = "InternalServerError",
                StatusCode = 500,
                Content = "Internal Server Error."
            };
        }

        public static IHttpResponse BadRequest() {
            //string content = File.ReadAllText("Resources/Pages/404.html");

            return new TextHttpResponse() {
                ReasonPhrase = "BadRequest",
                StatusCode = 400,
                Content = "Bad Request."
            };
        }

        public static IHttpResponse NotFound() {
            //string content = File.ReadAllText("Resources/Pages/404.html");

            return new TextHttpResponse() {
                ReasonPhrase = "NotFound",
                StatusCode = 404,
                Content = "Not Found."
            };
        }

        public static IHttpResponse MethodNotAllowed() {
            return new TextHttpResponse() {
                ReasonPhrase = "Method Not Allowed",
                StatusCode = 405,
                Content = "Method Not Allowed"
            };
        }

        public static IHttpResponse ServiceUnavailable() {
            return new TextHttpResponse() {
                ReasonPhrase = "ServiceUnavailable",
                StatusCode = 503,
                Content = "Service Unavailable"
            };
        }

    }
}
