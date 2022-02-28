﻿using System.Threading.Tasks;
using Bit.Core.Entities;
using Bit.Core.Enums;
using Bit.Core.Exceptions;
using Bit.Core.OrganizationFeatures.OrganizationSponsorships.FamiliesForEnterprise.Cloud;
using Bit.Core.Services;
using Bit.Test.Common.AutoFixture;
using Bit.Test.Common.AutoFixture.Attributes;
using NSubstitute;
using Xunit;

namespace Bit.Core.Test.OrganizationFeatures.OrganizationSponsorships.FamiliesForEnterprise
{
    [SutProviderCustomize]
    public class CloudSendSponsorshipOfferCommandTests : FamiliesForEnterpriseTestsBase
    {
        [Theory]
        [BitAutoData]
        public async Task ResendSponsorshipOffer_SponsoringOrgNotFound_ThrowsBadRequest(
            OrganizationUser orgUser, OrganizationSponsorship sponsorship,
            SutProvider<SendSponsorshipOfferCommand> sutProvider)
        {
            var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
                sutProvider.Sut.SendSponsorshipOfferAsync(null, orgUser, sponsorship, "test@bitwarden.com"));

            Assert.Contains("Cannot find the requested sponsoring organization.", exception.Message);
            await sutProvider.GetDependency<IMailService>()
                .DidNotReceiveWithAnyArgs()
                .SendFamiliesForEnterpriseOfferEmailAsync(default, default, default, default);
        }

        [Theory]
        [BitAutoData]
        public async Task ResendSponsorshipOffer_SponsoringOrgUserNotFound_ThrowsBadRequest(Organization org,
            OrganizationSponsorship sponsorship, SutProvider<SendSponsorshipOfferCommand> sutProvider)
        {
            var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
                sutProvider.Sut.SendSponsorshipOfferAsync(org, null, sponsorship, "test@bitwarden.com"));

            Assert.Contains("Only confirmed users can sponsor other organizations.", exception.Message);
            await sutProvider.GetDependency<IMailService>()
                .DidNotReceiveWithAnyArgs()
                .SendFamiliesForEnterpriseOfferEmailAsync(default, default, default, default);
        }

        [Theory]
        [BitAutoData]
        [BitMemberAutoData(nameof(NonConfirmedOrganizationUsersStatuses))]
        public async Task ResendSponsorshipOffer_SponsoringOrgUserNotConfirmed_ThrowsBadRequest(OrganizationUserStatusType status,
            Organization org, OrganizationUser orgUser, OrganizationSponsorship sponsorship,
            SutProvider<SendSponsorshipOfferCommand> sutProvider)
        {
            orgUser.Status = status;

            var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
                sutProvider.Sut.SendSponsorshipOfferAsync(org, orgUser, sponsorship, "test@bitwarden.com"));

            Assert.Contains("Only confirmed users can sponsor other organizations.", exception.Message);
            await sutProvider.GetDependency<IMailService>()
                .DidNotReceiveWithAnyArgs()
                .SendFamiliesForEnterpriseOfferEmailAsync(default, default, default, default);
        }

        [Theory]
        [BitAutoData]
        public async Task ResendSponsorshipOffer_SponsorshipNotFound_ThrowsBadRequest(Organization org,
            OrganizationUser orgUser,
            SutProvider<SendSponsorshipOfferCommand> sutProvider)
        {
            orgUser.Status = OrganizationUserStatusType.Confirmed;

            var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
                sutProvider.Sut.SendSponsorshipOfferAsync(org, orgUser, null, "test@bitwarden.com"));

            Assert.Contains("Cannot find an outstanding sponsorship offer for this organization.", exception.Message);
            await sutProvider.GetDependency<IMailService>()
                .DidNotReceiveWithAnyArgs()
                .SendFamiliesForEnterpriseOfferEmailAsync(default, default, default, default);
        }

        [Theory]
        [BitAutoData]
        public async Task ResendSponsorshipOffer_NoOfferToEmail_ThrowsBadRequest(Organization org,
            OrganizationUser orgUser, OrganizationSponsorship sponsorship,
            SutProvider<SendSponsorshipOfferCommand> sutProvider)
        {
            orgUser.Status = OrganizationUserStatusType.Confirmed;
            sponsorship.OfferedToEmail = null;

            var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
                sutProvider.Sut.SendSponsorshipOfferAsync(org, orgUser, sponsorship, "test@bitwarden.com"));

            Assert.Contains("Cannot find an outstanding sponsorship offer for this organization.", exception.Message);
            await sutProvider.GetDependency<IMailService>()
                .DidNotReceiveWithAnyArgs()
                .SendFamiliesForEnterpriseOfferEmailAsync(default, default, default, default);
        }
    }
}