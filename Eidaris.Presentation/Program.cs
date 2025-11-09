// Eidaris — Copyright (C) 2025
// Dorovskoy Alexey Vasilievich (One290 / 0ne290) <lenya.dorovskoy@mail.ru>
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version. See the LICENSE file for details.
// For full copyright and authorship information, see the COPYRIGHT file.

using Eidaris.Presentation.Extensions;
using Eidaris.Presentation.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Eidaris.Presentation;

internal static class Program
{
    private static void Main()
    {
        var services = new ServiceCollection();
        services.Compose();
        var serviceProvider = services.BuildServiceProvider();
        
        var eidarisApplication = serviceProvider.GetRequiredService<IEidarisApplication>();
        eidarisApplication.Run();
    }
}