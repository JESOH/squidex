﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Squidex.Infrastructure;
using Squidex.Shared.Users;

#pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body

namespace Squidex.Domain.Users
{
    public sealed class DefaultUserResolver : IUserResolver
    {
        private readonly IServiceProvider serviceProvider;

        public DefaultUserResolver(IServiceProvider serviceProvider)
        {
            Guard.NotNull(serviceProvider);

            this.serviceProvider = serviceProvider;
        }

        public async Task<(IUser? User, bool Created)> CreateUserIfNotExistsAsync(string email, bool invited)
        {
            Guard.NotNullOrEmpty(email);

            var created = false;

            using (var scope = serviceProvider.CreateScope())
            {
                var userFactory = scope.ServiceProvider.GetRequiredService<IUserFactory>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

                try
                {
                    var user = userFactory.Create(email);

                    var result = await userManager.CreateAsync(user);

                    if (result.Succeeded)
                    {
                        created = true;

                        var values = new UserValues { DisplayName = email, Invited = invited };

                        await userManager.UpdateAsync(user, values);
                    }
                }
                catch
                {
                }

                var found = await userManager.FindByEmailWithClaimsAsync(email);

                return (found, created);
            }
        }

        public async Task<IUser?> FindByIdAsync(string idOrEmail)
        {
            Guard.NotNullOrEmpty(idOrEmail);

            using (var scope = serviceProvider.CreateScope())
            {
                var userFactory = scope.ServiceProvider.GetRequiredService<IUserFactory>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

                if (userFactory.IsId(idOrEmail))
                {
                    return await userManager.FindByIdWithClaimsAsync(idOrEmail);
                }
                else
                {
                    return await userManager.FindByEmailWithClaimsAsync(idOrEmail);
                }
            }
        }

        public async Task<IUser?> FindByIdOrEmailAsync(string idOrEmail)
        {
            Guard.NotNullOrEmpty(idOrEmail);

            using (var scope = serviceProvider.CreateScope())
            {
                var userFactory = scope.ServiceProvider.GetRequiredService<IUserFactory>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

                if (userFactory.IsId(idOrEmail))
                {
                    return await userManager.FindByIdWithClaimsAsync(idOrEmail);
                }
                else
                {
                    return await userManager.FindByEmailWithClaimsAsync(idOrEmail);
                }
            }
        }

        public async Task<List<IUser>> QueryByEmailAsync(string email)
        {
            Guard.NotNullOrEmpty(email);

            using (var scope = serviceProvider.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

                var result = await userManager.QueryByEmailAsync(email);

                return result.OfType<IUser>().ToList();
            }
        }

        public async Task<Dictionary<string, IUser>> QueryManyAsync(string[] ids)
        {
            Guard.NotNull(ids);

            using (var scope = serviceProvider.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
                var userFactory = scope.ServiceProvider.GetRequiredService<IUserFactory>();

                ids = ids.Where(x => userFactory.IsId(x)).ToArray();

                var result = await userManager.QueryByIdsAync(ids);

                return result.OfType<IUser>().ToDictionary(x => x.Id);
            }
        }
    }
}
