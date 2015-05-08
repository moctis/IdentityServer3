﻿/*
 * Copyright 2014 Dominick Baier, Brock Allen
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Collections.Generic;

using Microsoft.Owin;
using Microsoft.Owin.Security.Facebook;
using Microsoft.Owin.Security.Google;
using Microsoft.Owin.Security.OpenIdConnect;
using Microsoft.Owin.Security.Twitter;
using Microsoft.Owin.Security.WsFederation;
using Owin;
using Thinktecture.IdentityServer.Core.Configuration;
using Thinktecture.IdentityServer.Core.Logging;
using Thinktecture.IdentityServer.Core.Services;
using Thinktecture.IdentityServer.Host;
using Thinktecture.IdentityServer.Host.Config;
using Thinktecture.IdentityServer.WsFederation.Configuration;
using Thinktecture.IdentityServer.WsFederation.Models;
using Thinktecture.IdentityServer.WsFederation.Services;

[assembly: OwinStartup("LocalTest", typeof(Startup_LocalTest))]

namespace Thinktecture.IdentityServer.Host
{
    public class Startup_LocalTest
    {
        public void Configuration(IAppBuilder app)
        {
            LogProvider.SetCurrentLogProvider(new DiagnosticsTraceLogProvider());
            LogProvider.SetCurrentLogProvider(new TraceSourceLogProvider());

            // uncomment to enable HSTS headers for the host
            // see: https://developer.mozilla.org/en-US/docs/Web/Security/HTTP_strict_transport_security
            app.UseHsts();

            app.Map("/core", coreApp =>
                {
                    //var factory = InMemoryFactory.Create(
                    //    users:   Users.Get(),                      
                    //    clients: Clients.Get(),
                    //    scopes:  Scopes.Get());
                    var factory = Factory.Configure("IdSvr3Config");

                    factory.CustomGrantValidator = 
                        new Registration<ICustomGrantValidator>(typeof(CustomGrantValidator));

                    factory.ConfigureClientStoreCache();
                    factory.ConfigureScopeStoreCache();
                    factory.ConfigureUserServiceCache();

                    var idsrvOptions = new IdentityServerOptions
                    {
                        Factory = factory,
                        SigningCertificate = Cert.Load(),

                        CorsPolicy = CorsPolicy.AllowAll,

                        AuthenticationOptions = new AuthenticationOptions 
                        {
                            IdentityProviders = ConfigureIdentityProviders,
                        },

                        LoggingOptions = new LoggingOptions
                        {
                            //EnableHttpLogging = true, 
                            //EnableWebApiDiagnostics = true,
                            //IncludeSensitiveDataInLogs = true
                        },

                        EventsOptions = new EventsOptions
                        {
                            RaiseFailureEvents = true,
                            RaiseInformationEvents = true,
                            RaiseSuccessEvents = true,
                            RaiseErrorEvents = true
                        }
                        ,

                        PluginConfiguration = ConfigureWsFederation,
                    };

                    coreApp.UseIdentityServer(idsrvOptions);
                });
        }

        private void ConfigureWsFederation(IAppBuilder pluginApp, IdentityServerOptions options)
        {
            var factory = new WsFederationServiceFactory(options.Factory);

            // data sources for in-memory services
            factory.Register(new Registration<IEnumerable<RelyingParty>>(RelyingParties.Get()));
            factory.RelyingPartyService = new Registration<IRelyingPartyService>(typeof(InMemoryRelyingPartyService));

            var wsFedOptions = new WsFederationPluginOptions
            {
                IdentityServerOptions = options,
                Factory = factory
            };
            wsFedOptions.EnableMetadataEndpoint = true;

            pluginApp.UseWsFederationPlugin(wsFedOptions);
        }
         

        public static void ConfigureIdentityProviders(IAppBuilder app, string signInAsType)
        {
            var google = new GoogleOAuth2AuthenticationOptions
            {
                AuthenticationType = "Google",
                Caption = "Google",
                SignInAsAuthenticationType = signInAsType,
                
                ClientId = "767400843187-8boio83mb57ruogr9af9ut09fkg56b27.apps.googleusercontent.com",
                ClientSecret = "5fWcBT0udKY7_b6E3gEiJlze"
            };
            app.UseGoogleAuthentication(google);

            var fb = new FacebookAuthenticationOptions
            {
                AuthenticationType = "Facebook",
                Caption = "Facebook",
                SignInAsAuthenticationType = signInAsType,
                
                AppId = "676607329068058",
                AppSecret = "9d6ab75f921942e61fb43a9b1fc25c63"
            };
            app.UseFacebookAuthentication(fb);

            var twitter = new TwitterAuthenticationOptions
            {
                AuthenticationType = "Twitter",
                Caption = "Twitter",
                SignInAsAuthenticationType = signInAsType,
                
                ConsumerKey = "N8r8w7PIepwtZZwtH066kMlmq",
                ConsumerSecret = "df15L2x6kNI50E4PYcHS0ImBQlcGIt6huET8gQN41VFpUCwNjM"
            };
            app.UseTwitterAuthentication(twitter);

            var adfs = new WsFederationAuthenticationOptions
            {
                AuthenticationType = "adfs",
                Caption = "ADFS",
                SignInAsAuthenticationType = signInAsType,

                //MetadataAddress = "https://adfs.leastprivilege.vm/federationmetadata/2007-06/federationmetadata.xml",
                MetadataAddress = "https://login.microsoftonline.com/70248591-7dbf-4c5c-a3e3-0a009f207bb2/federationmetadata/2007-06/federationmetadata.xml",
                Wtrealm = "urn:idsrv3"
            };
            app.UseWsFederationAuthentication(adfs);

            var aad = new OpenIdConnectAuthenticationOptions
            {
                AuthenticationType = "aad",
                Caption = "Azure AD",
                SignInAsAuthenticationType = signInAsType,

                Authority = "https://login.microsoftonline.com/70248591-7dbf-4c5c-a3e3-0a009f207bb2/",
                ClientId = "8e3c3b47-a9b4-4525-8bc3-2bb64630fc61",
                RedirectUri = "https://openid-draycir.azurewebsites.net/core/aadcb"
            };

            app.UseOpenIdConnectAuthentication(aad);
        }

    }
}