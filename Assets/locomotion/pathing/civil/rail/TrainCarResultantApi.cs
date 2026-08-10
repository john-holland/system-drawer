using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Fluent access to vehicles / limbs produced by a train car (unfold, park, refold).</summary>
public sealed class TrainCarResultantApi
{
    readonly TrainCarVehicleRagdoll _car;

    public TrainCarResultantApi(TrainCarVehicleRagdoll car) => _car = car;

    public VehicleQuery Vehicles() => new VehicleQuery(_car);
    public LimbQuery Limbs() => new LimbQuery(_car);
    public CombinedQuery All() => new CombinedQuery(_car);

    public sealed class VehicleQuery
    {
        readonly TrainCarVehicleRagdoll _car;
        bool _parkedOnly;
        string _kindFilter;

        public VehicleQuery(TrainCarVehicleRagdoll car) => _car = car;

        public VehicleQuery Parked()
        {
            _parkedOnly = true;
            return this;
        }

        public VehicleQuery OfKind(string vehicleIdPrefix)
        {
            _kindFilter = vehicleIdPrefix;
            return this;
        }

        public List<VehicleRagdoll> ToList()
        {
            var list = new List<VehicleRagdoll>();
            if (_car?.containmentBays == null) return list;
            for (int b = 0; b < _car.containmentBays.Count; b++)
            {
                var bay = _car.containmentBays[b];
                if (bay?.containedVehicles == null) continue;
                for (int i = 0; i < bay.containedVehicles.Count; i++)
                {
                    var v = bay.containedVehicles[i];
                    if (v == null) continue;
                    if (_parkedOnly && v.transform.parent == null)
                        continue;
                    if (!string.IsNullOrEmpty(_kindFilter)
                        && (v.vehicleId == null || !v.vehicleId.StartsWith(_kindFilter, StringComparison.OrdinalIgnoreCase)))
                        continue;
                    list.Add(v);
                }
            }
            return list;
        }

        public VehicleRagdoll FirstOrDefault()
        {
            var list = ToList();
            return list.Count > 0 ? list[0] : null;
        }
    }

    public sealed class LimbQuery
    {
        readonly TrainCarVehicleRagdoll _car;
        bool _unfoldedOnly;
        TrainCarLimbRole? _role;

        public LimbQuery(TrainCarVehicleRagdoll car) => _car = car;

        public LimbQuery Unfolded()
        {
            _unfoldedOnly = true;
            return this;
        }

        public LimbQuery OfRole(TrainCarLimbRole role)
        {
            _role = role;
            return this;
        }

        public List<TrainCarAmbulationLimb> ToList()
        {
            var list = new List<TrainCarAmbulationLimb>();
            if (_car?.limbs == null) return list;
            for (int i = 0; i < _car.limbs.Count; i++)
            {
                var limb = _car.limbs[i];
                if (limb == null) continue;
                if (_unfoldedOnly && !limb.IsUnfolded) continue;
                if (_role.HasValue && limb.role != _role.Value) continue;
                list.Add(limb);
            }
            return list;
        }
    }

    public sealed class CombinedQuery
    {
        readonly TrainCarVehicleRagdoll _car;

        public CombinedQuery(TrainCarVehicleRagdoll car) => _car = car;

        public bool Stable()
        {
            if (_car == null) return true;
            if (_car.lashRuntime != null)
                return _car.lashRuntime.IsStable;
            return _car.LastLashStable01 >= 0.5f;
        }
    }
}
