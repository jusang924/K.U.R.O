using Godot;
using System;

public partial class Enemy : CharacterBody3D
{
    [Export] public float Speed = 2.5f;
    [Export] public float DetectionRange = 5.0f;
    [Export] public float AttackRange = 1.5f;
    [Export] public float AttackDamage = 10.0f;
    [Export] public float AttackCooldown = 1.5f;
    [Export] public int MaxHealth = 50;
    [Export] public int ScoreValue = 10;
    
    private int _currentHealth;
    private float _attackTimer = 0.0f;
    private Player? _player;
    private RandomNumberGenerator _rng = new RandomNumberGenerator();
    
    public override void _Ready()
    {
        _currentHealth = MaxHealth;
        _rng.Randomize();
        
        // 找到玩家
        var parent = GetParent();
        if (parent != null)
        {
            _player = parent.GetNodeOrNull<Player>("Player");
        }
        
        // 设置敌人颜色（红色）
        var sprite = GetNode<Sprite3D>("Sprite3D");
        sprite.Modulate = new Color(1.0f, 0.3f, 0.3f); // 红色
    }
    
    public override void _PhysicsProcess(double delta)
    {
        if (_player == null) return;
        
        // 更新攻击冷却
        if (_attackTimer > 0)
        {
            _attackTimer -= (float)delta;
        }
        
        // 伪3D距离计算（只考虑XY平面）
        Vector2 playerPos2D = new Vector2(_player.GlobalPosition.X, _player.GlobalPosition.Y);
        Vector2 enemyPos2D = new Vector2(GlobalPosition.X, GlobalPosition.Y);
        float distanceToPlayer = playerPos2D.DistanceTo(enemyPos2D);
        
        Vector3 velocity = Velocity;
        
        // AI 行为（伪3D风格）
        if (distanceToPlayer <= DetectionRange)
        {
            // 在攻击范围内
            if (distanceToPlayer <= AttackRange)
            {
                // 停止移动并攻击
                velocity.X = 0;
                velocity.Y = 0;
                
                if (_attackTimer <= 0)
                {
                    AttackPlayer();
                }
            }
            else
            {
                // 追击玩家（只在XY平面移动）
                Vector2 direction2D = (playerPos2D - enemyPos2D).Normalized();
                velocity.X = direction2D.X * Speed;
                velocity.Y = direction2D.Y * Speed;
                
                // 根据X方向翻转敌人朝向
                if (direction2D.X != 0)
                {
                    var sprite = GetNode<Sprite3D>("Sprite3D");
                    sprite.FlipH = direction2D.X < 0;
                }
            }
        }
        else
        {
            // 待机
            velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed * 2);
            velocity.Y = Mathf.MoveToward(Velocity.Y, 0, Speed * 2);
        }
        
        // 限制Y轴范围
        float minY = -3.0f;
        float maxY = 3.0f;
        if (GlobalPosition.Y < minY || GlobalPosition.Y > maxY)
        {
            velocity.Y = 0;
            var pos = GlobalPosition;
            pos.Y = Mathf.Clamp(pos.Y, minY, maxY);
            GlobalPosition = pos;
        }
        
        Velocity = velocity;
        MoveAndSlide();
    }
    
    private void AttackPlayer()
    {
        _attackTimer = AttackCooldown;
        if (_player != null)
        {
            _player.TakeDamage((int)AttackDamage);
            GD.Print("Enemy attacked player!");
        }
        
        // 攻击动画效果
        var sprite = GetNode<Sprite3D>("Sprite3D");
        var originalScale = sprite.Scale;
        var tween = CreateTween();
        tween.TweenProperty(sprite, "scale", originalScale * 1.3f, 0.15);
        tween.TweenProperty(sprite, "scale", originalScale, 0.15);
    }
    
    public void TakeDamage(int damage)
    {
        _currentHealth -= damage;
        GD.Print($"Enemy took {damage} damage! Health: {_currentHealth}");
        
        // 受伤效果
        var sprite = GetNode<Sprite3D>("Sprite3D");
        var originalColor = sprite.Modulate;
        sprite.Modulate = new Color(1, 1, 1); // 白色闪烁
        
        var tween = CreateTween();
        tween.TweenInterval(0.1);
        tween.TweenCallback(Callable.From(() => sprite.Modulate = originalColor));
        
        if (_currentHealth <= 0)
        {
            Die();
        }
    }
    
    private void Die()
    {
        GD.Print("Enemy died!");
        
        // 给玩家加分
        if (_player != null)
        {
            _player.AddScore(ScoreValue);
        }
        
        // 死亡效果
        var sprite = GetNode<Sprite3D>("Sprite3D");
        var tween = CreateTween();
        tween.TweenProperty(sprite, "scale", Vector3.Zero, 0.3);
        tween.TweenCallback(Callable.From(QueueFree));
    }
}

