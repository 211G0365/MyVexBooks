using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace MyVexBooks.Models.Entities;

public partial class LibrosvexContext : DbContext
{
    public LibrosvexContext()
    {
    }

    public LibrosvexContext(DbContextOptions<LibrosvexContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Libros> Libros { get; set; }

    public virtual DbSet<Notificaciones> Notificaciones { get; set; }

    public virtual DbSet<Partelikes> Partelikes { get; set; }

    public virtual DbSet<Partes> Partes { get; set; }

    public virtual DbSet<Usuarios> Usuarios { get; set; }

    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Libros>(entity =>
        {
            entity.HasKey(e => e.IdLibro).HasName("PRIMARY");

            entity.ToTable("libros");

            entity.Property(e => e.IdLibro).HasColumnName("idLibro");
            entity.Property(e => e.Autor)
                .HasMaxLength(100)
                .HasColumnName("autor");
            entity.Property(e => e.Genero)
                .HasMaxLength(45)
                .HasColumnName("genero");
            entity.Property(e => e.PortadaUrl)
                .HasMaxLength(300)
                .HasColumnName("portadaURL");
            entity.Property(e => e.Sinopsis)
                .HasColumnType("text")
                .HasColumnName("sinopsis");
            entity.Property(e => e.Titulo)
                .HasMaxLength(255)
                .HasColumnName("titulo");
        });

        modelBuilder.Entity<Notificaciones>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("notificaciones");

            entity.HasIndex(e => e.Endpoint, "endpoint_UNIQUE").IsUnique();

            entity.Property(e => e.Activo)
                .HasDefaultValueSql("'1'")
                .HasColumnName("activo");
            entity.Property(e => e.Auth)
                .HasMaxLength(100)
                .HasColumnName("auth");
            entity.Property(e => e.Endpoint)
                .HasMaxLength(500)
                .HasColumnName("endpoint");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("fechaCreacion");
            entity.Property(e => e.P256dh)
                .HasMaxLength(200)
                .HasColumnName("p256dh");
            entity.Property(e => e.UserAgent)
                .HasMaxLength(500)
                .HasColumnName("user_agent");
        });

        modelBuilder.Entity<Partelikes>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("partelikes");

            entity.HasIndex(e => e.IdUsuario, "FK_ParteLikes_Usuarios");

            entity.HasIndex(e => new { e.IdParte, e.IdUsuario }, "UQ_ParteLikes").IsUnique();

            entity.Property(e => e.FechaLike)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp");

            entity.HasOne(d => d.IdParteNavigation).WithMany(p => p.Partelikes)
                .HasForeignKey(d => d.IdParte)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ParteLikes_Partes");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Partelikes)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ParteLikes_Usuarios");
        });

        modelBuilder.Entity<Partes>(entity =>
        {
            entity.HasKey(e => e.IdParte).HasName("PRIMARY");

            entity.ToTable("partes");

            entity.HasIndex(e => e.IdLibro, "fklibro_partes_idx");

            entity.Property(e => e.IdParte).HasColumnName("idParte");
            entity.Property(e => e.Contenido)
                .HasColumnType("text")
                .HasColumnName("contenido");
            entity.Property(e => e.IdLibro).HasColumnName("idLibro");
            entity.Property(e => e.Likes).HasColumnName("likes");
            entity.Property(e => e.NombreParte)
                .HasMaxLength(255)
                .HasColumnName("nombreParte");
            entity.Property(e => e.NumeroParte).HasColumnName("numeroParte");

            entity.HasOne(d => d.IdLibroNavigation).WithMany(p => p.Partes)
                .HasForeignKey(d => d.IdLibro)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fklibro_partes");
        });

        modelBuilder.Entity<Usuarios>(entity =>
        {
            entity.HasKey(e => e.IdUsuario).HasName("PRIMARY");

            entity.ToTable("usuarios");

            entity.HasIndex(e => e.Correo, "correo_UNIQUE").IsUnique();

            entity.Property(e => e.IdUsuario).HasColumnName("idUsuario");
            entity.Property(e => e.ContraseñaHash)
                .HasMaxLength(255)
                .HasColumnName("contraseñaHash");
            entity.Property(e => e.Correo).HasColumnName("correo");
            entity.Property(e => e.FechaRegistro)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("fechaRegistro");
            entity.Property(e => e.Nombre)
                .HasMaxLength(70)
                .HasColumnName("nombre");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
