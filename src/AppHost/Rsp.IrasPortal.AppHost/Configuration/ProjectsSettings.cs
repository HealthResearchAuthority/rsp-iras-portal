﻿namespace Rsp.IrasPortal.AppHost.Configuration;

internal struct ProjectsSettings
{
    public IrasServiceSettings IrasServiceSettings { get; set; }
    public UsersServiceSettings UsersServiceSettings { get; set; }
}