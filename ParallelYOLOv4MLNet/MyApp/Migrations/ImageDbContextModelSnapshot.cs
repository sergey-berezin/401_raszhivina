﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MyApp;

namespace MyApp.Migrations
{
    [DbContext(typeof(ImageDbContext))]
    partial class ImageDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "5.0.12");

            modelBuilder.Entity("MyApp.DetectedObject", b =>
                {
                    b.Property<int>("DetectedObjectId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int?>("DetailsObjectDetailsId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Label")
                        .HasColumnType("TEXT");

                    b.Property<int>("X1")
                        .HasColumnType("INTEGER");

                    b.Property<int>("X2")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Y1")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Y2")
                        .HasColumnType("INTEGER");

                    b.HasKey("DetectedObjectId");

                    b.HasIndex("DetailsObjectDetailsId");

                    b.ToTable("DetectedObjects");
                });

            modelBuilder.Entity("MyApp.ObjectDetails", b =>
                {
                    b.Property<int>("ObjectDetailsId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<byte[]>("Image")
                        .HasColumnType("BLOB");

                    b.HasKey("ObjectDetailsId");

                    b.ToTable("DetectedObjectDetails");
                });

            modelBuilder.Entity("MyApp.DetectedObject", b =>
                {
                    b.HasOne("MyApp.ObjectDetails", "Details")
                        .WithMany()
                        .HasForeignKey("DetailsObjectDetailsId");

                    b.Navigation("Details");
                });
#pragma warning restore 612, 618
        }
    }
}
