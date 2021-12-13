using System.Linq;
using Bit.Core.AccessPolicies;
using Bit.Core.Models.Table;
using Bit.Core.Settings;
using Bit.Core.Utilities;

namespace Bit.Core.OrganizationFeatures.Subscription
{
    public class OrganizationSubscriptionAccessPolicies : BaseAccessPolicies, IOrganizationSubscriptionAccessPolicies
    {
        readonly IGlobalSettings _globalSettings;

        public OrganizationSubscriptionAccessPolicies(IGlobalSettings globalSettings)
        {
            _globalSettings = globalSettings;
        }

        public AccessPolicyResult CanScale(Organization organization, int seatsToAdd)
        {
            if (OverrideExists())
            {
                return PermissionOverrides[nameof(CanScale)];
            }

            if (seatsToAdd < 1)
            {
                return Success;
            }

            if (_globalSettings.SelfHosted)
            {
                return Fail("Cannot autoscale on self-hosted instance.");
            }

            if (organization.Seats.HasValue &&
                organization.MaxAutoscaleSeats.HasValue &&
                organization.MaxAutoscaleSeats.Value < organization.Seats.Value + seatsToAdd)
            {
                return Fail($"Cannot invite new users. Seat limit has been reached.");
            }

            return Success;
        }

        public AccessPolicyResult CanAdjustSeats(Organization organization, int seatAdjustment,
            int currentUserCount)
        {
            if (organization.Seats == null)
            {
                return Fail("Organization has no seat limit, no need to adjust seats");
            }

            if (string.IsNullOrWhiteSpace(organization.GatewayCustomerId))
            {
                return Fail("No payment method found.");
            }

            if (string.IsNullOrWhiteSpace(organization.GatewaySubscriptionId))
            {
                return Fail("No subscription found.");
            }

            var plan = StaticStore.Plans.FirstOrDefault(p => p.Type == organization.PlanType);
            if (plan == null)
            {
                return Fail("Existing plan not found.");
            }

            if (!plan.HasAdditionalSeatsOption)
            {
                return Fail("Plan does not allow additional seats.");
            }

            var newSeatTotal = organization.Seats.Value + seatAdjustment;
            if (plan.BaseSeats > newSeatTotal)
            {
                return Fail($"Plan has a minimum of {plan.BaseSeats} seats.");
            }

            if (newSeatTotal <= 0)
            {
                return Fail("You must have at least 1 seat.");
            }

            var additionalSeats = newSeatTotal - plan.BaseSeats;
            if (plan.MaxAdditionalSeats.HasValue && additionalSeats > plan.MaxAdditionalSeats.Value)
            {
                return Fail($"Organization plan allows a maximum of " +
                    $"{plan.MaxAdditionalSeats.Value} additional seats.");
            }

            if (!organization.Seats.HasValue || organization.Seats.Value > newSeatTotal)
            {
                if (currentUserCount > newSeatTotal)
                {
                    return Fail($"Your organization currently has {currentUserCount} seats filled. " +
                        $"Your new plan only has ({newSeatTotal}) seats. Remove some users.");
                }
            }

            return Success;
        }
    }
}