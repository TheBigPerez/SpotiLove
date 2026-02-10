-- Drop all foreign key constraints
ALTER TABLE "Likes" DROP CONSTRAINT IF EXISTS "FK_Likes_Users_FromUserId";
ALTER TABLE "Likes" DROP CONSTRAINT IF EXISTS "FK_Likes_Users_ToUserId";
ALTER TABLE "Messages" DROP CONSTRAINT IF EXISTS "FK_Messages_Users_FromUserId";
ALTER TABLE "Messages" DROP CONSTRAINT IF EXISTS "FK_Messages_Users_ToUserId";
ALTER TABLE "MusicProfiles" DROP CONSTRAINT IF EXISTS "FK_MusicProfiles_Users_UserId";
ALTER TABLE "UserImages" DROP CONSTRAINT IF EXISTS "FK_UserImages_Users_UserId";
ALTER TABLE "UserSuggestionQueues" DROP CONSTRAINT IF EXISTS "FK_UserSuggestionQueues_Users_UserId";
ALTER TABLE "UserSuggestionQueues" DROP CONSTRAINT IF EXISTS "FK_UserSuggestionQueues_Users_SuggestedUserId";

-- Convert all ID columns to UUID
ALTER TABLE "Users" ALTER COLUMN "Id" TYPE uuid USING "Id"::uuid;
ALTER TABLE "MusicProfiles" ALTER COLUMN "Id" TYPE uuid USING "Id"::uuid;
ALTER TABLE "MusicProfiles" ALTER COLUMN "UserId" TYPE uuid USING "UserId"::uuid;
ALTER TABLE "UserImages" ALTER COLUMN "Id" TYPE uuid USING "Id"::uuid;
ALTER TABLE "UserImages" ALTER COLUMN "UserId" TYPE uuid USING "UserId"::uuid;
ALTER TABLE "Likes" ALTER COLUMN "FromUserId" TYPE uuid USING "FromUserId"::uuid;
ALTER TABLE "Likes" ALTER COLUMN "ToUserId" TYPE uuid USING "ToUserId"::uuid;
ALTER TABLE "UserSuggestionQueues" ALTER COLUMN "UserId" TYPE uuid USING "UserId"::uuid;
ALTER TABLE "UserSuggestionQueues" ALTER COLUMN "SuggestedUserId" TYPE uuid USING "SuggestedUserId"::uuid;
ALTER TABLE "Messages" ALTER COLUMN "Id" TYPE uuid USING "Id"::uuid;
ALTER TABLE "Messages" ALTER COLUMN "FromUserId" TYPE uuid USING "FromUserId"::uuid;
ALTER TABLE "Messages" ALTER COLUMN "ToUserId" TYPE uuid USING "ToUserId"::uuid;

-- Recreate foreign key constraints
ALTER TABLE "Likes" ADD CONSTRAINT "FK_Likes_Users_FromUserId" FOREIGN KEY ("FromUserId") REFERENCES "Users"("Id") ON DELETE CASCADE;
ALTER TABLE "Likes" ADD CONSTRAINT "FK_Likes_Users_ToUserId" FOREIGN KEY ("ToUserId") REFERENCES "Users"("Id") ON DELETE CASCADE;
ALTER TABLE "Messages" ADD CONSTRAINT "FK_Messages_Users_FromUserId" FOREIGN KEY ("FromUserId") REFERENCES "Users"("Id") ON DELETE CASCADE;
ALTER TABLE "Messages" ADD CONSTRAINT "FK_Messages_Users_ToUserId" FOREIGN KEY ("ToUserId") REFERENCES "Users"("Id") ON DELETE CASCADE;
ALTER TABLE "MusicProfiles" ADD CONSTRAINT "FK_MusicProfiles_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE;
ALTER TABLE "UserImages" ADD CONSTRAINT "FK_UserImages_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE;
ALTER TABLE "UserSuggestionQueues" ADD CONSTRAINT "FK_UserSuggestionQueues_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE;
ALTER TABLE "UserSuggestionQueues" ADD CONSTRAINT "FK_UserSuggestionQueues_Users_SuggestedUserId" FOREIGN KEY ("SuggestedUserId") REFERENCES "Users"("Id") ON DELETE CASCADE;
