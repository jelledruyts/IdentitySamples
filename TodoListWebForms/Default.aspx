<%@ Page Async="true" Title="Account" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="TodoListWebForms._Default" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <h2>Identity Information</h2>
    <div class="panel panel-primary">
        <div class="panel-heading">
            <h4 class="panel-title">
                <a data-toggle="collapse" href="#application-panel-<%: identity.Application.GetHashCode() %>"><%: identity.Application %></a>
            </h4>
        </div>
        <div id="application-panel-<%: @identity.Application.GetHashCode() %>" class="panel-collapse collapse in">
            <div class="panel-body">
                <div class="panel-group">
                    <!-- Identity Information -->
                    <div class="panel panel-default">
                        <div class="panel-heading">
                            <h4 class="panel-title">
                                <a data-toggle="collapse" href="#identity-panel-<%: @identity.Application.GetHashCode() %>">Identity</a>
                            </h4>
                        </div>
                        <div id="identity-panel-<%: identity.Application.GetHashCode() %>" class="panel-collapse collapse in">
                            <div class="panel-body">
                                <dl class="dl-horizontal">
                                    <dt>Source</dt>
                                    <dd><%: identity.Source %></dd>
                                    <dt>Is Authenticated</dt>
                                    <dd><%: identity.IsAuthenticated.ToString().ToLowerInvariant() %></dd>
                                    <dt>Name</dt>
                                    <dd><%: identity.Name %></dd>
                                    <dt>Authentication Type</dt>
                                    <dd><%: identity.AuthenticationType %></dd>
                                    <% if (identity.RoleNames != null && identity.RoleNames.Any()) { %>
                                        <dt>Application Roles</dt>
                                        <dd><%: string.Join(", ", identity.RoleNames) %></dd>
                                    <% } %>
                                    <% if (identity.GroupNames != null && identity.GroupNames.Any()) { %>
                                        <dt>Groups</dt>
                                        <dd><%: string.Join(", ", identity.GroupNames) %></dd>
                                    <% } %>
                                </dl>
                            </div>
                        </div>
                    </div>
                    <!-- Claims Information -->
                    <div class="panel panel-default">
                        <div class="panel-heading">
                            <h4 class="panel-title">
                                <a data-toggle="collapse" href="#claims-panel-<%: identity.Application.GetHashCode() %>">
                                    Claims
                                </a>
                            </h4>
                        </div>
                        <div id="claims-panel-<%: identity.Application.GetHashCode() %>" class="panel-collapse collapse in">
                            <div class="panel-body">
                                <table class="table table-bordered table-striped table-responsive">
                                    <thead>
                                        <tr>
                                            <th>Claim Type</th>
                                            <th>Claim Value</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        <% foreach (var claim in identity.Claims) { %>
                                            <tr>
                                                <td><%: claim.Type %></td>
                                                <td><abbr title="<%: claim.Remark %>"><%: claim.Value %></abbr></td>
                                            </tr>
                                        <% } %>
                                    </tbody>
                                </table>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</asp:Content>
