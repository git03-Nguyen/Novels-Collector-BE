## ✨ Novels Collector Website (Backend) - Group 12 ✨
#### 📖 *A platform for searching and reading novels from multiple websites using hot-plugins in ASP.NET Core.* 
*This is only the back-end part of the project. Please refer to https://github.com/thienan2003bt/Novels-Collector-FE for the front-end part. Thank you!*
### 1. Course information:
- **Course name:** Software Design
- **Course ID:** CSC13010
- **Class:** CQ21_3

### 2. Team information:
- **Group name:** *Group12*
- **Members:**

| No. | Student ID |       Full name      |             Email             |    Role    |
|:---:|:----------:|----------------------|-------------------------------|------------|
|  1  |  21120036  | Triệu Hoàng Thiên Ân | 21120036@student.hcmus.edu.vn | Front-end  |
|  2  |  21120105  | Trương Thành Nhân    | 21120105@student.hcmus.edu.vn | BA, Tester |
|  3  |  21120171  | Nguyễn Đình Ánh      | 21120171@student.hcmus.edu.vn | Back-end   |
|  4  |  21120172  | Nguyễn Tuấn Đạt      | 21120172@student.hcmus.edu.vn | Back-end   |
|  5  |  21120177  | Lê Minh Huy          | 21120177@student.hcmus.edu.vn | Front-end  |

### 3. Technology stack information:
- **Vietnamese name:** Website Tổng hợp và đọc tiểu thuyết trực tuyến
- **Front-end:** React
- **Back-end:** ASP.NET Core
- **Database:** MongoDB

### 4. Installation guide:
To get the project up and running for the first time, follow these steps:
1. **Build the solution:**\
	Navigate to the root directory of the project and run the following command to build the solution:
	```bash
	dotnet build
	```
2. **Run the script to copy plugins into the core app:**\
	With local run, we have to install the plugins by hand:
	```bash
	bash ./plugin-scripts/copy_source_plugins.sh
	bash ./plugin-scripts/copy_exporter_plugins.sh
	```
3. **Data migration**\
	Import *.json* files in `mongodb-migrations/` folder to initialize the database. 
4. **Run the project:**\
	Start the project by using:
	```bash
	dotnet run --project ./NovelsCollector.Core/NovelsCollector.Core.csproj
	```

Alternatively, you can also build and run the solution with the GUI by opening `BE.NovelsCollector.sln` in Visual Studio IDE.

