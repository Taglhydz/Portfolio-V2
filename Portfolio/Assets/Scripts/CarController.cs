using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CarController : MonoBehaviour
{
    private Rigidbody playerRB;
    public WheelColliders colliders;
    public WheelMeshes wheelMeshes;
    public WheelParticles wheelParticles;
    public float gasInput;
    public float brakeInput;
    public float steeringInput;
    public GameObject smokePrefab;
    public float motorPower;
    public float brakePower;
    public float handBrakePower;
    public float slipAngle;
    private float speed;
    public AnimationCurve steeringCurve;
    private float currentSteeringAngle;
    public float steeringSpeed = 5f;

    private InputAction accelerateAction;
    private InputAction reverseAction;
    private InputAction steerLeftAction;
    private InputAction steerRightAction;
    private InputAction handbrakeAction;

    void Awake()
    {
        playerRB = GetComponent<Rigidbody>();

        accelerateAction = new InputAction("Accelerate", InputActionType.Button);
        accelerateAction.AddBinding("<Keyboard>/z");
        accelerateAction.AddBinding("<Keyboard>/w");
        accelerateAction.AddBinding("<Keyboard>/upArrow");

        reverseAction = new InputAction("Reverse", InputActionType.Button);
        reverseAction.AddBinding("<Keyboard>/s");
        reverseAction.AddBinding("<Keyboard>/downArrow");

        steerLeftAction = new InputAction("SteerLeft", InputActionType.Button);
        steerLeftAction.AddBinding("<Keyboard>/q");
        steerLeftAction.AddBinding("<Keyboard>/a");
        steerLeftAction.AddBinding("<Keyboard>/leftArrow");

        steerRightAction = new InputAction("SteerRight", InputActionType.Button);
        steerRightAction.AddBinding("<Keyboard>/d");
        steerRightAction.AddBinding("<Keyboard>/rightArrow");

        handbrakeAction = new InputAction("Handbrake", InputActionType.Button);
        handbrakeAction.AddBinding("<Keyboard>/space");
    }

    void OnEnable()
    {
        accelerateAction.Enable();
        reverseAction.Enable();
        steerLeftAction.Enable();
        steerRightAction.Enable();
        handbrakeAction.Enable();
    }

    void OnDisable()
    {
        accelerateAction.Disable();
        reverseAction.Disable();
        steerLeftAction.Disable();
        steerRightAction.Disable();
        handbrakeAction.Disable();
    }

    void Start()
    {
        InstantiateParticles();
    }

    void Update()
    {
        speed = playerRB.linearVelocity.magnitude;
        CheckInput();
        ApplyMotor();
        ApplySteering();
        ApplyBrake();
        ApplyWheelMeshes();
        CheckParticles();
    }

    void CheckInput()
    {
        gasInput = accelerateAction.IsPressed() ? 1f : 0f;
        gasInput -= reverseAction.IsPressed() ? 1f : 0f;
        steeringInput = steerRightAction.IsPressed() ? 1f : 0f;
        steeringInput -= steerLeftAction.IsPressed() ? 1f : 0f;

        slipAngle = Vector3.Angle(transform.forward, playerRB.linearVelocity - transform.forward);

        float movingDirection = Vector3.Dot(transform.forward, playerRB.linearVelocity);
        if (movingDirection < -0.5f && gasInput > 0)
            brakeInput = Mathf.Abs(gasInput);
        else if (movingDirection > 0.5f && gasInput < 0)
            brakeInput = Mathf.Abs(gasInput);
        else
            brakeInput = 0;
    }

    void ApplyMotor()
    {
        colliders.FRWheel.motorTorque = 0;
        colliders.FLWheel.motorTorque = 0;
        colliders.RRWheel.motorTorque = motorPower * gasInput;
        colliders.RLWheel.motorTorque = motorPower * gasInput;
    }

    void ApplySteering()
    {
        float targetAngle = steeringInput * steeringCurve.Evaluate(speed);

        if (Mathf.Abs(slipAngle) < 120f)
            targetAngle += slipAngle;

        targetAngle = Mathf.Clamp(targetAngle, -90f, 90f);

        currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetAngle, Time.deltaTime * steeringSpeed);

        colliders.FRWheel.steerAngle = currentSteeringAngle;
        colliders.FLWheel.steerAngle = currentSteeringAngle;
    }

    void ApplyBrake()
    {
        bool handbrake = handbrakeAction.IsPressed();

        if (handbrake)
        {
            colliders.RRWheel.brakeTorque = handBrakePower;
            colliders.RLWheel.brakeTorque = handBrakePower;
            colliders.FRWheel.brakeTorque = 0;
            colliders.FLWheel.brakeTorque = 0;
            SetRearFriction(0.3f);
        }
        else
        {
            SetRearFriction(1f);
            colliders.RRWheel.brakeTorque = 0;
            colliders.RLWheel.brakeTorque = 0;

            if (brakeInput > 0)
            {
                colliders.FRWheel.brakeTorque = brakePower * brakeInput * 0.7f;
                colliders.FLWheel.brakeTorque = brakePower * brakeInput * 0.7f;
                colliders.RRWheel.brakeTorque = brakePower * brakeInput * 0.3f;
                colliders.RLWheel.brakeTorque = brakePower * brakeInput * 0.3f;
            }
            else
            {
                colliders.FRWheel.brakeTorque = 0;
                colliders.FLWheel.brakeTorque = 0;
            }
        }
    }

    void SetRearFriction(float stiffness)
    {
        WheelFrictionCurve friction;

        friction = colliders.RRWheel.sidewaysFriction;
        friction.stiffness = stiffness;
        colliders.RRWheel.sidewaysFriction = friction;

        friction = colliders.RLWheel.sidewaysFriction;
        friction.stiffness = stiffness;
        colliders.RLWheel.sidewaysFriction = friction;
    }

    void InstantiateParticles()
    {
        wheelParticles.FRWheel = Instantiate(smokePrefab, colliders.FRWheel.transform.position, Quaternion.identity, colliders.FRWheel.transform).GetComponent<ParticleSystem>();
        wheelParticles.FLWheel = Instantiate(smokePrefab, colliders.FLWheel.transform.position, Quaternion.identity, colliders.FLWheel.transform).GetComponent<ParticleSystem>();
        wheelParticles.RRWheel = Instantiate(smokePrefab, colliders.RRWheel.transform.position, Quaternion.identity, colliders.RRWheel.transform).GetComponent<ParticleSystem>();
        wheelParticles.RLWheel = Instantiate(smokePrefab, colliders.RLWheel.transform.position, Quaternion.identity, colliders.RLWheel.transform).GetComponent<ParticleSystem>();
    }

    void ApplyWheelMeshes()
    {
        UpdateWheel(colliders.FRWheel, wheelMeshes.FRWheel);
        UpdateWheel(colliders.FLWheel, wheelMeshes.FLWheel);
        UpdateWheel(colliders.RRWheel, wheelMeshes.RRWheel);
        UpdateWheel(colliders.RLWheel, wheelMeshes.RLWheel);
    }

    void CheckParticles()
    {
        WheelHit[] wheelHits = new WheelHit[4];
        colliders.FRWheel.GetGroundHit(out wheelHits[0]);
        colliders.FLWheel.GetGroundHit(out wheelHits[1]);
        colliders.RRWheel.GetGroundHit(out wheelHits[2]);
        colliders.RLWheel.GetGroundHit(out wheelHits[3]);

        float slipAllowance = 0.5f;

        ToggleParticle(wheelParticles.FRWheel, wheelHits[0], slipAllowance);
        ToggleParticle(wheelParticles.FLWheel, wheelHits[1], slipAllowance);
        ToggleParticle(wheelParticles.RRWheel, wheelHits[2], slipAllowance);
        ToggleParticle(wheelParticles.RLWheel, wheelHits[3], slipAllowance);
    }

    void ToggleParticle(ParticleSystem ps, WheelHit hit, float allowance)
    {
        if (Mathf.Abs(hit.sidewaysSlip) + Mathf.Abs(hit.forwardSlip) > allowance)
            ps.Play();
        else
            ps.Stop();
    }

    void UpdateWheel(WheelCollider coll, MeshRenderer wheelMesh)
    {
        coll.GetWorldPose(out Vector3 position, out Quaternion quat);
        wheelMesh.transform.position = position;
        wheelMesh.transform.rotation = quat;
    }
}

[System.Serializable]
public class WheelColliders
{
    public WheelCollider FRWheel;
    public WheelCollider FLWheel;
    public WheelCollider RRWheel;
    public WheelCollider RLWheel;
}

[System.Serializable]
public class WheelMeshes
{
    public MeshRenderer FRWheel;
    public MeshRenderer FLWheel;
    public MeshRenderer RRWheel;
    public MeshRenderer RLWheel;
}

[System.Serializable]
public class WheelParticles
{
    public ParticleSystem FRWheel;
    public ParticleSystem FLWheel;
    public ParticleSystem RRWheel;
    public ParticleSystem RLWheel;
}
