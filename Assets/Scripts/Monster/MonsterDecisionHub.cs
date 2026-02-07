using System;
using UnityEngine;

public sealed class MonsterDecisionHub
{
    readonly MonsterContext ctx;

    float _detectGate;
    float _returnGate;
    float _disengageHold;

    public Vector3 LastNoisePos { get; private set; }
    public Transform LastSeenItem { get; private set; }
    public bool IsFastReturnRequested { get; private set; }

    public MonsterDecisionHub(MonsterContext ctx) { this.ctx = ctx; ResetAll(); }

    public void ResetAll()
    {
        _detectGate = _returnGate = _disengageHold = 0f;
        LastNoisePos = Vector3.zero;
        LastSeenItem = null;
        IsFastReturnRequested = false;
    }

    public Route Decide(float dt)
    {

        if (ctx.MoveLocked) return Route.None;

        return Route.None; // Idle À¯Áö
    }

    static bool Gate(ref float acc, float dt, float need)
    {
        acc += dt;
        if (acc >= need) { acc = 0f; return true; }
        return false;
    }
}