using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
	public PlayerDataWithDash Data;

	#region COMPONENTS
    public Rigidbody RB { get; private set; }
	#endregion

	#region STATE PARAMETERS
	public bool IsFacingRight { get; private set; }
	public bool IsJumping { get; private set; }
	public bool IsWallJumping { get; private set; }
	public bool IsDashing { get; private set; }
	public bool IsSliding { get; private set; }

	public float LastOnGroundTime { get; private set; }
	public float LastOnWallTime { get; private set; }
	public float LastOnWallRightTime { get; private set; }
	public float LastOnWallLeftTime { get; private set; }

	//Jump
	private int _jumpsLeft;
	private bool _isJumpCut;
	private bool _isJumpFalling;

	//Wall Jump
	private float _wallJumpStartTime;
	private int _lastWallJumpDir;

    //Dash
    [SerializeField] private ParticleSystem dashEffect;
    private int _dashesLeft;
	private bool _dashRefilling;
	private Vector2 _lastDashDir;
	private bool _isDashAttacking;

	private float _jumpTimeCounter;

	private float _wallJumpMovementLockTime = 0.25f;
	private float _wallJumpMovementLockCounter;
	private int _wallJumpDirection;

	#endregion

	#region INPUT PARAMETERS
	private Vector3 _moveInput;

	public float LastPressedJumpTime { get; private set; }
	public float LastPressedDashTime { get; private set; }
	#endregion

	#region CHECK PARAMETERS
	[Header("Checks")] 
	[SerializeField] private Transform _groundCheckPoint;
	[SerializeField] private Vector3 _groundCheckSize = new Vector3(0.49f, 0.03f, 0.49f);
	[Space(5)]
	[SerializeField] private Transform _frontWallCheckPoint;
	[SerializeField] private Transform _backWallCheckPoint;
	[SerializeField] private Vector3 _wallCheckSize = new Vector3(0.5f, 1f, 0.5f);
    #endregion

    #region LAYERS & TAGS
    [Header("Layers & Tags")]
	[SerializeField] private LayerMask _groundLayer;
	[SerializeField] private LayerMask _wallLayer;
	#endregion

    private void Awake()
	{
		RB = GetComponent<Rigidbody>();
	}

	private void Start()
	{
		RB.useGravity = true;
		IsFacingRight = true;
        _jumpsLeft = Data.maxJumps;
    }

	private void Update()
	{
        #region TIMERS
        LastOnGroundTime -= Time.deltaTime;
		LastOnWallTime -= Time.deltaTime;
		LastOnWallRightTime -= Time.deltaTime;
		LastOnWallLeftTime -= Time.deltaTime;

		LastPressedJumpTime -= Time.deltaTime;
		LastPressedDashTime -= Time.deltaTime;
		#endregion

		#region INPUT HANDLER
		_moveInput.x = Input.GetAxisRaw("Horizontal");
		_moveInput.z = Input.GetAxisRaw("Vertical");

		if (_moveInput.x != 0)
			CheckDirectionToFace(_moveInput.x > 0);

		if(Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.J))
        {
			Debug.Log("space is pressed");
			OnJumpInput();
        }

		if (Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.C) || Input.GetKeyUp(KeyCode.J))
		{
			OnJumpUpInput();
		}

		if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.K))
		{
			OnDashInput();
		}
		#endregion

		#region COLLISION CHECKS
		if (!IsDashing && !IsJumping)
		{
			//Ground Check
			if (Physics.OverlapBox(_groundCheckPoint.position, _groundCheckSize/2, Quaternion.identity, _groundLayer).Length > 0 && !IsJumping)
			{
				LastOnGroundTime = Data.coyoteTime;
			}		

			// Simplified wall checks
			bool touchingFrontWall = Physics.OverlapBox(_frontWallCheckPoint.position, _wallCheckSize/2, Quaternion.identity, _wallLayer).Length > 0;
			bool touchingBackWall = Physics.OverlapBox(_backWallCheckPoint.position, _wallCheckSize/2, Quaternion.identity, _wallLayer).Length > 0;

			// Right wall check
			if ((touchingFrontWall && IsFacingRight) || (touchingBackWall && !IsFacingRight))
			{
				LastOnWallRightTime = Data.coyoteTime;
			}

			// Left wall check
			if ((touchingFrontWall && !IsFacingRight) || (touchingBackWall && IsFacingRight))
			{
				LastOnWallLeftTime = Data.coyoteTime;
			}

			LastOnWallTime = Mathf.Max(LastOnWallLeftTime, LastOnWallRightTime);
		}
		#endregion

		#region JUMP CHECKS
		if (IsJumping && RB.linearVelocity.y < 0)
		{
			IsJumping = false;
			

			if(!IsWallJumping)
				_isJumpFalling = true;
		}

		if (IsWallJumping && Time.time - _wallJumpStartTime > Data.wallJumpTime)
		{
			IsWallJumping = false;
		}

		if (LastOnGroundTime > 0 && !IsJumping && !IsWallJumping)
        {
			_isJumpCut = false;

			if(!IsJumping)
				_isJumpFalling = false;
		}

		if (!IsDashing)
		{
			if (LastPressedJumpTime > 0)
			{
				// Prioritize wall jump over normal jump by checking it first
				if (CanWallJump())
				{
					IsWallJumping = true;
					IsJumping = false;
					_isJumpCut = false;
					_isJumpFalling = false;

					_wallJumpStartTime = Time.time;
					_lastWallJumpDir = (LastOnWallRightTime > 0) ? -1 : 1;

					WallJump(_lastWallJumpDir);
					LastPressedJumpTime = 0; // Clear the jump input
				}
				// Only try normal jump if we're not wall jumping
				else if (CanJump() && !IsWallJumping)
				{
					Jump();
				}
			}
		}
		if (IsJumping && Input.GetKey(KeyCode.Space) && !CanWallJump())
		{
			if (_jumpTimeCounter > 0)
			{
				RB.AddForce(Vector3.up * Data.jumpForce * 0.5f, ForceMode.Force);
				_jumpTimeCounter -= Time.deltaTime;
			}
		}
		#endregion

		#region DASH CHECKS
		if (CanDash() && LastPressedDashTime > 0)
		{
			Sleep(Data.dashSleepTime); 
			if (_moveInput != Vector3.zero)
				_lastDashDir = _moveInput;
			else
				_lastDashDir = IsFacingRight ? Vector2.right : Vector2.left;

			IsDashing = true;
			IsJumping = false;
			IsWallJumping = false;
			_isJumpCut = false;

			StartCoroutine(nameof(StartDash), _lastDashDir);
		}
		#endregion

		#region SLIDE CHECKS
		if (CanSlide() && ((LastOnWallLeftTime > 0 && _moveInput.x < 0) || (LastOnWallRightTime > 0 && _moveInput.x > 0)))
			IsSliding = true;
		else
			IsSliding = false;
        #endregion

        #region GRAVITY
        if (!_isDashAttacking)
        {
            if (IsSliding)
            {
                RB.useGravity = false;
                RB.linearVelocity = new Vector3(RB.linearVelocity.x, 0.01f, RB.linearVelocity.z);
            }
            else if (RB.linearVelocity.y < 0) // When falling
            {
                RB.useGravity = true;
                
                if (Input.GetKey(KeyCode.S))
                {
                    RB.AddForce(Vector3.down * Data.gravityScale * 3f, ForceMode.Force);
                    RB.linearVelocity = new Vector3(RB.linearVelocity.x,
                        Mathf.Max(RB.linearVelocity.y, -Data.maxFastFallSpeed),
                        RB.linearVelocity.z);
                }
                else
                {
                    // Normal falling
                    RB.linearVelocity = new Vector3(RB.linearVelocity.x,
                        Mathf.Max(RB.linearVelocity.y, -Data.maxFallSpeed),
                        RB.linearVelocity.z);
                }
            }
            else if (_isJumpCut)
            {
                RB.useGravity = true;
                RB.linearVelocity = new Vector3(RB.linearVelocity.x,
                    Mathf.Max(RB.linearVelocity.y, -Data.maxFallSpeed),
                    RB.linearVelocity.z);
            }
            else
            {
                RB.useGravity = true;
            }
        }
        else
        {
            RB.useGravity = false;
        }
        #endregion
    }

    private void FixedUpdate()
	{
		//Handle Run
		if (!IsDashing)
		{
			if (IsWallJumping)
				Run(Data.wallJumpRunLerp);
			else
				Run(1);
		}
		else if (_isDashAttacking)
		{
			Run(Data.dashEndRunLerp);
		}

		//Handle Slide
		if (IsSliding)
			Slide();
    }

    #region INPUT CALLBACKS
    public void OnJumpInput()
	{
		LastPressedJumpTime = Data.jumpInputBufferTime;
	}

	public void OnJumpUpInput()
	{
		if (CanJumpCut() || CanWallJumpCut())
			_isJumpCut = true;
			Vector3 currentVelocity = RB.linearVelocity;
			if(currentVelocity.y > 0)
			{
				RB.linearVelocity = new Vector3(currentVelocity.x, currentVelocity.y * 0.1f, currentVelocity.z);
			}
	}

	public void OnDashInput()
	{
		LastPressedDashTime = Data.dashInputBufferTime;
	}
    #endregion

    #region GENERAL METHODS
    public void SetGravityScale(float scale)
	{
        RB.useGravity = false;
        Physics.gravity = new Vector3(0, -9.81f * scale, 0);
	}

	private void Sleep(float duration)
    {
		StartCoroutine(nameof(PerformSleep), duration);
    }

	private IEnumerator PerformSleep(float duration)
    {
		Time.timeScale = 0;
		yield return new WaitForSecondsRealtime(duration);
		Time.timeScale = 1;
	}
    #endregion

	//MOVEMENT METHODS
    #region RUN METHODS
    private void Run(float lerpAmount)
	{
		float targetSpeedX = _moveInput.x;
		if (_wallJumpMovementLockCounter > 0)
		{
			_wallJumpMovementLockCounter -= Time.deltaTime;
			if (Mathf.Sign(targetSpeedX) == -Mathf.Sign(_wallJumpDirection))
			{
				targetSpeedX = 0;
			}
		}
		Vector3 targetVelocity = new Vector3(targetSpeedX, 0, _moveInput.z) * Data.runMaxSpeed;
		Vector3 currentVelocity = new Vector3(RB.linearVelocity.x, 0, RB.linearVelocity.z);
		targetVelocity = Vector3.Lerp(currentVelocity, targetVelocity, lerpAmount);

		#region Calculate AccelRate
		float accelRate;
		if (LastOnGroundTime > 0)
			accelRate = (Mathf.Abs(targetVelocity.magnitude) > 0.01f) ? Data.runAccelAmount : Data.runDeccelAmount;
		else
			accelRate = (Mathf.Abs(targetVelocity.magnitude) > 0.01f) ? Data.runAccelAmount * Data.accelInAir : Data.runDeccelAmount * Data.deccelInAir;
		#endregion

		#region Add Bonus Jump Apex Acceleration
		if ((IsJumping || IsWallJumping || _isJumpFalling) && Mathf.Abs(RB.linearVelocity.y) < Data.jumpHangTimeThreshold)
		{
			accelRate *= Data.jumpHangAccelerationMult;
			targetVelocity *= Data.jumpHangMaxSpeedMult;
		}
		#endregion

		#region Conserve Momentum
		if(Data.doConserveMomentum && Mathf.Abs(RB.linearVelocity.x) > Mathf.Abs(targetVelocity.x) && Mathf.Sign(RB.linearVelocity.x) == Mathf.Sign(targetVelocity.x) && Mathf.Abs(targetVelocity.x) > 0.01f && LastOnGroundTime < 0)
		{
			accelRate = 0; 
		}
		#endregion

		Vector3 speedDif = targetVelocity - currentVelocity;
		Vector3 movement = speedDif * accelRate;

		RB.AddForce(movement, ForceMode.Force);
	}

	private void Turn()
	{
		Vector3 scale = transform.localScale; 
		scale.x *= -1;
		transform.localScale = scale;

		IsFacingRight = !IsFacingRight;
	}
    #endregion

    #region JUMP METHODS
    private void Jump()
    {


        LastPressedJumpTime = 0;
        
        if (LastOnGroundTime > 0)
        {
            // Ground jump
            IsJumping = true;
            _jumpsLeft = Data.maxJumps - 1;

        }
        else
        {
            // Air jump
            IsJumping = true;
            _jumpsLeft--;

        }

        LastOnGroundTime = 0;
        RB.linearVelocity = new Vector3(RB.linearVelocity.x, 0, RB.linearVelocity.z);
        RB.AddForce(Vector3.up * Data.jumpForce, ForceMode.Impulse);

        _jumpTimeCounter = Data.maxJumpTime;
    }
    #endregion

    #region WALL JUMP
    private void WallJump(int dir)
	{
		LastPressedJumpTime = 0;
		LastOnGroundTime = 0;
		LastOnWallRightTime = 0;
		LastOnWallLeftTime = 0;
		
		_wallJumpDirection = dir;
		_wallJumpMovementLockCounter = _wallJumpMovementLockTime;
		
		Vector3 force = new Vector3(Data.wallJumpForce.x * dir, Data.wallJumpForce.y, 0);
		RB.linearVelocity = Vector3.zero;
		RB.AddForce(force, ForceMode.Impulse);
	}
	#endregion

	#region DASH METHODS
	//Dash Coroutine
	private IEnumerator StartDash(Vector2 dir)
	{
		if (IsFacingRight)
		{
			dashEffect.transform.rotation = Quaternion.Euler(0, 0, 135);
            dashEffect.Play();
		}
		else
		{
			dashEffect.transform.rotation = Quaternion.Euler(0, 0, 0);
            dashEffect.Play();
        }
		LastOnGroundTime = 0;
		LastPressedDashTime = 0;

		float startTime = Time.time;

		_dashesLeft--;
		_isDashAttacking = true;

		SetGravityScale(0);

		while (Time.time - startTime <= Data.dashAttackTime)
		{
			RB.linearVelocity = dir.normalized * Data.dashSpeed;
			yield return null;
		}

		startTime = Time.time;

		_isDashAttacking = false;

		//Begins the "end" of our dash where we return some control to the player but still limit run acceleration (see Update() and Run())
		SetGravityScale(1);
		RB.linearVelocity = Data.dashEndSpeed * dir.normalized;

		while (Time.time - startTime <= Data.dashEndTime)
		{
			yield return null;
		}

		//Dash over
		IsDashing = false;
	}

	//Short period before the player is able to dash again
	private IEnumerator RefillDash(int amount)
	{
		_dashRefilling = true;
		yield return new WaitForSeconds(Data.dashRefillTime);
		_dashRefilling = false;
		_dashesLeft = Mathf.Min(Data.dashAmount, _dashesLeft + 1);
	}
	#endregion

	#region OTHER MOVEMENT METHODS
	private void Slide()
	{
		float speedDif = Data.slideSpeed - RB.linearVelocity.y;	
		float movement = speedDif * Data.slideAccel;
		movement = Mathf.Clamp(movement, -Mathf.Abs(speedDif)  * (1 / Time.fixedDeltaTime), Mathf.Abs(speedDif) * (1 / Time.fixedDeltaTime));

		RB.AddForce(movement * Vector3.up);
	}
    #endregion


    #region CHECK METHODS
    public void CheckDirectionToFace(bool isMovingRight)
	{
		if (isMovingRight != IsFacingRight)
			Turn();
	}

    private bool CanJump()
    {
        if (LastOnGroundTime > 0)
            return true;
        if (_jumpsLeft > 0 && !IsJumping && !IsWallJumping)
            return true;

        return false;
    }

    private bool CanWallJump()
    {
        return LastPressedJumpTime > 0 && LastOnWallTime > 0 && LastOnGroundTime <= 0 && 
               (!IsWallJumping || Time.time - _wallJumpStartTime > Data.wallJumpTime);
    }

    private bool CanJumpCut()
    {
		return IsJumping && RB.linearVelocity.y > 0;
    }

	private bool CanWallJumpCut()
	{
		return IsWallJumping && RB.linearVelocity.y > 0;
	}

	private bool CanDash()
	{
		if (!IsDashing && _dashesLeft < Data.dashAmount && LastOnGroundTime > 0 && !_dashRefilling)
		{
			StartCoroutine(nameof(RefillDash), 1);
		}
		return _dashesLeft > 0;
	}

	public bool CanSlide()
    {
		if (LastOnWallTime > 0 && !IsJumping && !IsWallJumping && !IsDashing && LastOnGroundTime <= 0)
			return true;
		else
			return false;
	}
    #endregion


    #region EDITOR METHODS
    private void OnDrawGizmosSelected()
    {
		Gizmos.color = Color.green;
		Gizmos.DrawWireCube(_groundCheckPoint.position, _groundCheckSize);
		Gizmos.color = Color.blue;
		Gizmos.DrawWireCube(_frontWallCheckPoint.position, _wallCheckSize);
		Gizmos.DrawWireCube(_backWallCheckPoint.position, _wallCheckSize);
	}
    #endregion
}