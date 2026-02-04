# BoredGamers – Database DDL

```sql
CREATE TABLE Users (
    ID INT PRIMARY KEY,
    FirstName VARCHAR(50),
    LastName VARCHAR(50),
    UserName VARCHAR(50) UNIQUE NOT NULL,
    Email VARCHAR(100) NOT NULL,
    Password VARCHAR(255) NOT NULL,
    Birthday DATE
);

CREATE TABLE Games (
    ID INT PRIMARY KEY,
    Title VARCHAR(100) NOT NULL,
    Description TEXT,
    Player_Count INT,
    Play_Time INT,
    Difficulty VARCHAR(20),
    Image_URL VARCHAR(255),
    Min_Age INT,
    Rating INT,
    BGG_ID INT UNIQUE
);
```
