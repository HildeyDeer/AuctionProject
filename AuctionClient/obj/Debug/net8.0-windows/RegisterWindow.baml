   M S B A M L   `   `   `  ����  AuctionClient"    AuctionClient.RegisterWindow   5   	   W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	     � Title$  Регистрация��5   	   $	��380��6   $	��350��6*    � 
ResizeMode$ NoResize=�66   ���#2E2E2E � 	Resources 5         RoundedButtonStyle �� 
TargetType �� Property �� Value3  -System.Windows.Media.Effects.DropShadowEffect   Color   	Direction   ShadowDepth	   Opacity
   
BlurRadius �� 
TargetType �� CornerRadius �� Padding �� Triggers P� Property P� Value  	TextStyle��%�  &	        &	 K    �� 5   
   " ��6+   .�5      ��  � ��$ 10q�6(   �� 5	       � ��$ SemiBold"�6+   �� 5
       � �� �#1F77B46+   �� 5       � ��
 �����6+   �� 5       � ��
 ���� 6,   �� 5       � ��$ 0q�60   �� 5       �@J�Effect 5        5      $ Black��6'   $	 270Q�65   $ 4Q�6E   $		 0.3Q�6U   $
 8Q�6c   5      �� 5       � �� 5      �� 5      " ��6&   .R�5      �� #��z��6!   $ 10��6K   # z��6]   ._�5      �� $��Center=�6/   $��Center=�6L   5      5      5       5      P� 5       � ��$
 True��6/   . �5      ��  � �� �#0F5B8F63   �� 5        �@J�Effect 5!        5"      $ Black��6/   $	 270Q�6=   $ 6Q�6M   $		 0.5Q�6]   $
 10Q�6k   5$      5%      5'      �� 5*   
   " ��6"   .�5+      ��  � ��$ 14(�6)   �� 5,       � �� �#D0D0D06+   5-      5.      .��50      � $��0,0,0,5q�6   .+�51   
   �� $
��Auto=�6   $��Disabled=�6:   .��52      �� $��Center=�6   $	��Top=�66   $	��300��6N   .+�54      �� $��Регистрация��6   $��20(�6/   $
��Bold"�6=   $��10q�6O    �� TextAlignment$ Center=�6[   ���#FFFFFF6r   �� 57      $��
Логин��6   #��[ 6)   $��5q�6L   �� 58      -   5         ��Name$ UsernameBox��58      $	��200��6/   $��35��6;   $��5q�6G   $
��10,5q�6R   $��16(�6a   ���#444444$��1q�6�   ���#3A3A3A$��Center=�6�   
�������6�   �� 5;      $��Пароль��6   #��[ 6*   $��5q�6M   G� 5<      -   5         G�Name$ PasswordBox��5<      $	��200��63   $��35��6?   $��5q�6K   $
��10,5q�6V   $��16(�6e   ���#444444$��1q�6�   ���#3A3A3A$��Center=�6�   
�������6�   �� 5?      $��Email��6   #��[ 6)   $��5q�6L   �� 5@      -   5        $ EmailBox��5@      $	��200��6,   $��35��68   $��5q�6D   $
��10,5q�6O   $��16(�6^   ���#444444$��1q�6�   ���#3A3A3A$��Center=�6�   
�������6�   �� 5C      $��
Адрес��6   #��[ 6)   $��5q�6L   �� 5D      -   5        $ 
AddressBox��5D      $	��200��6.   $��35��6:   $��5q�6F   $
��10,5q�6Q   $��16(�6`   ���#444444$��1q�6�   ���#3A3A3A$��Center=�6�   
�������6�   �� 5G      $��Номер карты��6   #��[ 6/   $��5q�6R   �� 5H      -   5        $ CardNumberBox��5H      $	��200��61   $��35��6=   $��5q�6I   $
��10,5q�6T   $��16(�6c   ���#444444$��1q�6�   ���#3A3A3A$��Center=�6�   
�������6�   �� 5K      $��Фото профиля��6   #��[ 60   $��5q�6S   �� 5L      -   5        $ ProfileImagePath��5L      $	��200��64   $��35��6@   $��5q�6L   $
��10,5q�6W   $��16(�6f   ���#444444$��1q�6�   ���#3A3A3A$��Center=�6�   
������� �� 
IsReadOnly$
 True��6�   �� 5O      -   5        $��Выбрать фото��5O      $	��200��6Q   $��40��6]   $��5q�6i   #��[  6t   $��Center=�6�   $��Center=�6�   $��16(�6�   �� 5R      -   5         ��Name$ ProfilePreview��5R      $	��100��60   $	��100��6<   $��5q�6I   $��UniformToFill=�6T   $y�	Collapsed=�6l   $��Center=�6�   $��Center=�6�   �� 5U      -	   5        $*��$Зарегистрироваться��5U      $	��200��6M   $��45��6Y   $��5q�6e   #��[  6p   $��Center=�6�   $��Center=�6�   $��16(�6�   5V      5W      5X      5Y      