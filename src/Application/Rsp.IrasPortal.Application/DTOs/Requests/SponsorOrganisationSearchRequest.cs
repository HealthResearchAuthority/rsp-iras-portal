﻿namespace Rsp.IrasPortal.Application.DTOs.Requests;

public class SponsorOrganisationSearchRequest
{
    public string? SearchQuery { get; set; }
    public List<string> Country { get; set; } = [];
    public List<string> RtsIds { get; set; } = [];
    public bool? Status { get; set; }
}