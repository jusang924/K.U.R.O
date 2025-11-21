using Godot;
using System;

public partial class Player : CharacterBody3D
{
    [Export] public float Speed = 5.0f;
    [Export] public float AttackDamage = 25.0f;
    [Export] public float AttackCooldown = 0.5f;
    [Export] public int MaxHealth = 100;
    
    private int _currentHealth;
    private float _attackTimer = 0.0f;
    private Area3D _attackArea = null!;
    private Label _statsLabel = null!;
    private int _score = 0;
    private MeshInstance3D _attackVisualization = null!;
    
    public override void _Ready()
    {
        _currentHealth = MaxHealth;
        _attackArea = GetNode<Area3D>("AttackArea");
        _statsLabel = GetNode<Label>("../UI/PlayerStats");
        UpdateStatsUI();
        
        // 设置玩家颜色（蓝色）
        var sprite = GetNode<Sprite3D>("Sprite3D");
        sprite.Modulate = new Color(0.3f, 0.5f, 1.0f); // 蓝色
        
        // 创建攻击范围可视化（伪3D横向卷轴）
        _attackVisualization = new MeshInstance3D();
        var boxMesh = new BoxMesh();
        boxMesh.Size = new Vector3(1.5f, 1.0f, 0.3f); // 横向宽，深度薄
        _attackVisualization.Mesh = boxMesh;
        
        var vizMaterial = new StandardMaterial3D();
        vizMaterial.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        vizMaterial.AlbedoColor = new Color(1, 0, 0, 0.3f); // 半透明红色
        _attackVisualization.MaterialOverride = vizMaterial;
        
        AddChild(_attackVisualization);
        _attackVisualization.Position = new Vector3(0.8f, 0, 0);  // 角色右侧
        _attackVisualization.Visible = false; // 默认隐藏
    }
    
    public override void _PhysicsProcess(double delta)
    {
        // 更新攻击冷却
        if (_attackTimer > 0)
        {
            _attackTimer -= (float)delta;
        }
        
        // 伪3D移动输入（失落城堡风格）
        Vector3 velocity = Velocity;
        Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
        
        // X轴：左右移动，Y轴：前后深度（伪3D）
        if (inputDir != Vector2.Zero)
        {
            velocity.X = inputDir.X * Speed;
            velocity.Y = -inputDir.Y * Speed;  // Y轴控制深度（前后）
            
            // 根据移动方向翻转角色朝向（只左右翻转）
            if (inputDir.X != 0)
            {
                var sprite = GetNode<Sprite3D>("Sprite3D");
                sprite.FlipH = inputDir.X < 0; // 向左时翻转
            }
        }
        else
        {
            velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed * 2);
            velocity.Y = Mathf.MoveToward(Velocity.Y, 0, Speed * 2);
        }
        
        // 限制Y轴范围（不要走出地面边界）
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
        
        // 攻击输入
        if (Input.IsActionJustPressed("attack") && _attackTimer <= 0)
        {
            Attack();
        }
        
        // 退出游戏
        if (Input.IsActionJustPressed("ui_cancel"))
        {
            GetTree().Quit();
        }
    }
    
    private void Attack()
    {
        _attackTimer = AttackCooldown;
        
        GD.Print($"=== Player attacking! ===");
        
        int hitCount = 0;
        var sprite = GetNode<Sprite3D>("Sprite3D");
        float facingDirection = sprite.FlipH ? -1 : 1; // FlipH=true 时朝左(-1), false时朝右(1)
        
        // 伪3D攻击判定（横向卷轴风格）
        var parent = GetParent();
        foreach (Node child in parent.GetChildren())
        {
            if (child is Enemy enemy)
            {
                // 2D平面距离检测
                Vector2 playerPos = new Vector2(GlobalPosition.X, GlobalPosition.Y);
                Vector2 enemyPos = new Vector2(enemy.GlobalPosition.X, enemy.GlobalPosition.Y);
                Vector2 toEnemy = enemyPos - playerPos;
                float distance = toEnemy.Length();
                
                // 攻击范围判定
                float attackRange = 1.5f;
                float attackDepth = 1.0f; // Y轴容差
                
                // 检查是否在攻击范围内
                bool inRange = distance <= attackRange;
                
                // 检查是否在正确的方向（左或右）
                bool correctDirection = (facingDirection > 0 && toEnemy.X > 0) || 
                                       (facingDirection < 0 && toEnemy.X < 0);
                
                // 检查深度差异不要太大
                bool inDepth = Mathf.Abs(toEnemy.Y) <= attackDepth;
                
                GD.Print($"Enemy at {enemyPos}, distance: {distance:F2}, direction OK: {correctDirection}, depth OK: {inDepth}");
                
                if (inRange && correctDirection && inDepth)
                {
                    enemy.TakeDamage((int)AttackDamage);
                    hitCount++;
                    GD.Print($"Hit enemy! Distance: {distance:F2}");
                }
            }
        }
        
        if (hitCount == 0)
        {
            GD.Print("No enemies hit!");
        }
        
        // 视觉反馈 - 显示攻击范围
        if (_attackVisualization != null)
        {
            _attackVisualization.Visible = true;
            // 根据朝向翻转攻击可视化
            _attackVisualization.Scale = new Vector3(facingDirection, 1, 1);
            var vizTween = CreateTween();
            vizTween.TweenInterval(0.2);
            vizTween.TweenCallback(Callable.From(() => _attackVisualization.Visible = false));
        }
        
        // 攻击动画 - 放大缩小
        var originalScale = sprite.Scale;
        var tween = CreateTween();
        tween.TweenProperty(sprite, "scale", originalScale * 1.2f, 0.1);
        tween.TweenProperty(sprite, "scale", originalScale, 0.1);
    }
    
    public void TakeDamage(int damage)
    {
        _currentHealth -= damage;
        _currentHealth = Mathf.Max(_currentHealth, 0);
        UpdateStatsUI();
        
        GD.Print($"Player took {damage} damage! Health: {_currentHealth}");
        
        // 受伤闪烁效果
        var sprite = GetNode<Sprite3D>("Sprite3D");
        var originalColor = sprite.Modulate;
        sprite.Modulate = new Color(1, 0, 0); // 红色
        
        var tween = CreateTween();
        tween.TweenInterval(0.1);
        tween.TweenCallback(Callable.From(() => sprite.Modulate = originalColor));
        
        if (_currentHealth <= 0)
        {
            Die();
        }
    }
    
    public void AddScore(int points)
    {
        _score += points;
        UpdateStatsUI();
    }
    
    private void UpdateStatsUI()
    {
        if (_statsLabel != null)
        {
            _statsLabel.Text = $"Player HP: {_currentHealth}\nScore: {_score}";
        }
    }
    
    private void Die()
    {
        GD.Print("Player died! Game Over!");
        GetTree().ReloadCurrentScene();
    }
}

