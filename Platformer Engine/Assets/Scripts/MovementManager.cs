using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementManager : MonoBehaviour {

    private const int RIGHT = 1, LEFT = -1, UP = 1, DOWN = -1, IDLE = 0;

    #region Collision Info
    public struct CollisionInfo {
        public RaycastHit2D right;
        public RaycastHit2D left;
        public RaycastHit2D up;
        public RaycastHit2D down;
        public CollisionInfo(RaycastHit2D r, RaycastHit2D l, RaycastHit2D u, RaycastHit2D d)
        {
            right = r; left = l; up = u; down = d;
        }

        public bool collidedRight
        {
            get { return right.collider != null; }
        }

        public bool collidedLeft
        {
            get { return left.collider != null; }
        }

        public bool collidedUp
        {
            get { return up.collider != null; }
        }

        public bool collidedDown
        {
            get { return down.collider != null; }
        }

    };

    

    private CollisionInfo _collisionInfo = new CollisionInfo( );
    public CollisionInfo collisionInfo
    {
        get { return _collisionInfo; }
    }

    #endregion

    #region Movement
    [Header("Horizontal Movement")]
    public float MaxSpeed;
    public float GroundAcceleration;
    public float AirAcceleration;
    public float GroundFriction;
    public float AirFriction;

    [Header("Vertical Movement")]
    public float jumpHeight;    
    public float jumpTime;    
    public float TerminalVelocity;    

    [Header("Wall Jumps")]
    public float WallAirFriction;
    public float WallFriction;
    public float WallJumpVelocity;

    private float GlobalAirFriction;

    private int horizontalDirection = 0;
    private int verticalDirection = 0;
    private bool applyGravity = true;
    private bool jumped = false;
    private bool jumping = false;
    private bool stopJump = false;

    private float gravity;
    private float jumpVelocity;
    private bool stopUpdate;

    #endregion


    [Header("Collision")]
    public int verticalRays = 4;
    public int horizontalRays = 4;
    public float margin = 0.01f;

    private bool fallThrough = false;

    private Rect boxCollider;  

    /* *
     * You can set your own function to handle velocity
     * for things that don't behave like normal physics bodies
     * (like a floating platform for example)
     * */
    public delegate Vector2 MovementAction();
    public MovementAction velocityFunction;

    // Read only variable 
    private bool falling = false;
    public bool _falling
    {
        get { return falling; }
    }

    private bool grounded = false;
    public bool _grounded
    {
        get { return grounded; }
    }

    private Vector2 _velocity;
    public Vector2 velocity
    {
        get { return _velocity; }
    }

    private Vector2 _direction;
    public Vector2 direction
    {
        get { return _direction; }
    }

    // Use this for initialization
    void Start() {
        _velocity = new Vector2(0, 0);
        _direction = new Vector2(0, 0);

        velocityFunction = setVelocity;
        GlobalAirFriction = AirFriction;
    }

    void FixedUpdate()
    {
        
        GetCollisionInfo(ref _collisionInfo, boxCollider);

        // Zero the velocity
        if (stopUpdate)
        {
            _velocity = new Vector2(0, 0);            
            return;
        }


        // Update direction and velocity value for the user
        _velocity = velocityFunction();
        _direction = new Vector2(Maths.Sign(_velocity.x), Maths.Sign(_velocity.y));        

        if (_velocity.y < 0)
        {
            falling = true;
        }

        HandleCollisions();
        
        transform.Translate(_velocity);        

        // Set direction again after moving. If the player collided, the direction can change now
        _direction = new Vector2(Maths.Sign(_velocity.x), Maths.Sign(_velocity.y));
        Debug.Log(_velocity.x);
        // Reset variables when grounded
        if(grounded)
        {
            GlobalAirFriction = AirFriction;
            jumping = false;
        }
    }


    /* *
     * Handle Movement Collision
     * */
    private void HandleCollisions()
    {       
        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        boxCollider = new Rect(
                collider.bounds.min.x,
                collider.bounds.min.y,
                collider.bounds.size.x,
                collider.bounds.size.y
            );

        Vector3 startpoint, endpoint, dir;
        RaycastHit2D hitInfo;
        float distance;

        /* *
         * HORIZONTAL MOVEMENT
         * */
        startpoint  = new Vector2(boxCollider.center.x, boxCollider.yMin);
        endpoint    = new Vector2(boxCollider.center.x, boxCollider.yMax);
        distance    = boxCollider.width / 2 + Mathf.Abs(_velocity.x) + margin;
        dir         = new Vector2(_direction.x, 0);

        if (CastRays(out hitInfo, startpoint, endpoint, dir, distance, horizontalRays))
        {
            // Put it inside a variable because it might be overriden to 0 and we need it to translate back
            float prevX = _velocity.x;
            transform.Translate(new Vector2(prevX, 0));

            RaycastHit2D vHitDown;
            bool SlopeHit = CastRays(
                                         out vHitDown,                                          // return value
                                         new Vector2(boxCollider.xMin, boxCollider.center.y),   // startpoint
                                         new Vector2(boxCollider.xMax, boxCollider.center.y),   // endpoint
                                         Vector3.down,                                          // direction
                                         boxCollider.height / 2 + margin,                       // distance
                                         verticalRays                                           // # of rays
                                     );

            switch(hitInfo.transform.tag)
            {
                case "Ground":
                // fallthrough
                case "Wall":
                    transform.Translate(dir * (hitInfo.distance - boxCollider.width / 2 - margin));
                    _velocity = new Vector2(0, _velocity.y);
                    break;
                case "Slope":
                    if(SlopeHit)
                    {
                        transform.Translate(Vector3.down * (vHitDown.distance - boxCollider.height / 2 - margin));
                    }
                    break;
                case "JumpThrough":
                    // do nothing
                    break;
            }

            transform.Translate(new Vector2(-prevX, 0));
        }


        /* *
         * VERTICAL MOVEMENT
         * */
        startpoint  = new Vector2(boxCollider.xMin, boxCollider.center.y);
        endpoint    = new Vector2(boxCollider.xMax, boxCollider.center.y);
        distance    = boxCollider.height / 2 + Mathf.Abs(_velocity.y) + margin;
        dir         = new Vector2(0, _direction.y);

        if (CastRays(out hitInfo, startpoint, endpoint, dir, distance, verticalRays))
        {
            grounded = _velocity.y < 0;
            falling = _velocity.y < 0 && !grounded;

            switch(hitInfo.transform.tag)
            {
                case "Wall":
                    // fallthrough
                case "Slope":
                    // fallthrough
                case "Ground":
                    transform.Translate(dir * (hitInfo.distance - boxCollider.height / 2 - margin));
                    _velocity = new Vector2(_velocity.x, 0);
                    break;
                case "JumpThrough":
                    if(velocity.y < 0 && hitInfo.distance > boxCollider.height / 2 && !fallThrough)
                    {
                        transform.Translate(dir * (hitInfo.distance - boxCollider.height / 2 - margin));
                        _velocity = new Vector2(_velocity.x, 0);
                    }
                    break;                
            }          
        }
        else
        {
            grounded = false;
        }

        fallThrough = false;
    }

    Vector2 setVelocity()
    {
        float xVelocity = 0, yVelocity = 0;
        float Friction = grounded ? GroundFriction : AirFriction;
        float Acceleration = grounded ? GroundAcceleration : AirAcceleration;
   
        // Update Jump velocity and gravity
        float FrameTime = jumpTime / Time.fixedDeltaTime;
        gravity = 2 * jumpHeight / (FrameTime * FrameTime);
        jumpVelocity = Mathf.Sqrt(2 * gravity * jumpHeight);

        /* *
         * Horizontal Direction
         * */
        if ((velocity.x < 0 && horizontalDirection == RIGHT) || (velocity.x > 0 && horizontalDirection == LEFT) 
            || horizontalDirection == IDLE || Maths.Sign(velocity.x) > Maths.Sign(MaxSpeed))
        {
            xVelocity = Maths.Lerp(velocity.x, 0, Friction);
        }
        else
        {
            xVelocity = Maths.Lerp(velocity.x, horizontalDirection * MaxSpeed, Acceleration);
        }

        /* *
         * Vertical Direction
         * Controlling the value of the vertical movement is a responsability of the user
         * If the user doesn't set the jump value back, it will keep applying vertical velocity
         * */
        if (applyGravity)
        {
            // If falling and touching a wall, slow down gravity
            if ((collisionInfo.collidedLeft || collisionInfo.collidedRight) && velocity.y < 0)
                gravity = Mathf.Clamp(gravity - WallFriction, 0, gravity);

            yVelocity = Maths.Lerp(_velocity.y, -TerminalVelocity, gravity);            
        }
        else
        {
            if ((velocity.y < 0 && verticalDirection == UP) || (velocity.y > 0 && verticalDirection == DOWN) || verticalDirection == IDLE)
            {
                yVelocity = Maths.Lerp(velocity.y, 0, Friction);
            }
            else
            {
                yVelocity = Maths.Lerp(velocity.y, verticalDirection * MaxSpeed, GroundAcceleration);
            }
        }

        // Reset jump state
        jumped = false;
        stopJump = false;

        return new Vector2(xVelocity, yVelocity);
    }

    #region Ray Handling
    void GetCollisionInfo(ref CollisionInfo info, Rect boxCollider, float rayDist = 0.01f)
    {
        Vector2 startpoint, endpoint;
        float distance;

        /* *
         * Vertical Rays (Up and Down)
         * */
        startpoint = new Vector2(boxCollider.xMin, boxCollider.center.y);
        endpoint = new Vector2(boxCollider.xMax, boxCollider.center.y);
        distance = boxCollider.height / 2 + margin + rayDist;

        CastRays(out info.down, startpoint, endpoint, Vector2.down, distance, verticalRays);
        CastRays(out info.up, startpoint, endpoint, Vector2.up, distance, verticalRays);

        /* *
         * Horizontal Rays (Left and Right)
         * */
        startpoint = new Vector2(boxCollider.center.x, boxCollider.yMin);
        endpoint = new Vector2(boxCollider.center.x, boxCollider.yMax);
        distance = boxCollider.width / 2 + margin + rayDist;

        CastRays(out info.left, startpoint, endpoint, Vector2.left, distance, verticalRays);
        CastRays(out info.right, startpoint, endpoint, Vector2.right, distance, verticalRays);
    }

    bool CastRays(out RaycastHit2D hitInfo, Vector2 startpoint, Vector2 endpoint, Vector2 direction, float distance, int quantity = 4)
    {

        RaycastHit2D[] hits = new RaycastHit2D[quantity];
        float length = Vector2.Distance(startpoint, endpoint);
        bool collided = false;
        for (int i = 0; i < quantity; i++)
        {
            float lerpAmount = (float)i / ((float)quantity - 1.0f);           
            Vector2 origin = Vector2.Lerp(startpoint, endpoint, lerpAmount);            

            Ray ray = new Ray(origin, direction);

            Debug.DrawRay(origin, direction * distance, Color.red);
            RaycastHit2D result = Physics2D.Raycast(origin, direction, distance);
            if (result.collider != null)
            {
                collided = true;                
            }

            hits[i] = result;
        }        

        hitInfo = GetClosestHit(hits);
        return collided;
    }

    bool CastRays(out List<RaycastHit2D> hitInfo, Vector2 startpoint, Vector2 endpoint, Vector2 direction, float distance, int quantity = 4)
    {

        hitInfo = new List<RaycastHit2D>();
        float length = Vector2.Distance(startpoint, endpoint);
        bool collided = false;
        for (int i = 0; i < quantity; i++)
        {
            float lerpAmount = (float)i / ((float)quantity - 1.0f);
            Vector2 origin = Vector2.Lerp(startpoint, endpoint, lerpAmount);

            Ray ray = new Ray(origin, direction);

            Debug.DrawRay(origin, direction * distance, Color.red);
            RaycastHit2D result = Physics2D.Raycast(origin, direction, distance);
            if (result.collider != null)
            {
                collided = true;
                hitInfo.Add(result);
            }
        }
        
        return collided;
    }

    RaycastHit2D GetClosestHit(RaycastHit2D[] hits)
    {
        RaycastHit2D result = new RaycastHit2D();

        foreach(RaycastHit2D hit in hits)
        {
            if (result.collider == null || (hit.collider != null && hit.distance < result.distance))
                result = hit;
        }

        return result;
    }

    #endregion

    #region User Functions

    // User function to set movement state
    public void SetDirection(int horizontalDirection, int verticalDirection = 0)
    {
        this.horizontalDirection = Maths.Sign(horizontalDirection);
        this.verticalDirection = Maths.Sign(verticalDirection);
    }

    public void Stop()
    {
        stopUpdate = true;
    }

    public void Resume()
    {
        stopUpdate = false;
    }  

    public void CutJump(float factor)
    {
        if(jumping && factor >= 0.0001f)
        {
            _velocity.y /= factor;
        }
    }

    public void Jump()
    {        
        _velocity.y = jumpVelocity;

        this.jumped = true;
        this.jumping = true;
    }   

    public void WallJump(int direction = 0)
    {
        // If the user doesn't provide direction, apply horizontal velocity
        // to the opposite direction the body is colliding
        direction = direction == 0 ? (collisionInfo.collidedLeft ? 1 : (collisionInfo.collidedRight ? -1 : 0)) : direction;

        // If direction wasn't provided AND body isn't colliding with anything, do nothing
        if (direction == 0)
            return;

        _velocity.x = direction * WallJumpVelocity;
        _velocity.y = jumpVelocity;
        GlobalAirFriction = WallAirFriction;
    }

    public void FallThrough()
    {
        if(collisionInfo.down && collisionInfo.down.transform.tag == "JumpThrough")
        {
            fallThrough = true;
        }
    }

    public void ApplyGravity(bool apply)
    {
        applyGravity = apply;
    }

    #endregion  

}
