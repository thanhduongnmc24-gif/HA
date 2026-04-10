git add . && git commit -m "Cập nhật" && git push origin main
dotnet publish  xuất file exe
//tạo thư mục out để lưu file exe
    dotnet publish HA/HA.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o out 
# 1. Xóa sạch các thư mục chứa file .exe rác để nó không vướng bận nữa
rm -rf out bin obj "D:\HA"

# 2. Quay ngược thời gian Git về lúc chưa bị lỗi (Code vẫn giữ nguyên không mất chữ nào)
git reset origin/main

# 3. Tạo "tấm khiên" .gitignore để từ nay về sau Git tự động làm ngơ, không thèm đưa mấy thư mục build vào danh sách đẩy lên mạng nữa
echo "out/" >> .gitignore
echo "bin/" >> .gitignore
echo "obj/" >> .gitignore
echo "D:\HA/" >> .gitignore

# 4. Đóng gói lại đúng chuẩn và đẩy thẳng lên mây
git add .
git commit -m "Cập nhật giao diện Material Design và loại bỏ file build nặng"
git push origin main