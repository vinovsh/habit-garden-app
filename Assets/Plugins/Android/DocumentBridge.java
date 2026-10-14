package com.rendermind.habitgarden;

import android.app.Activity;
import android.app.Fragment;
import android.content.Intent;
import android.os.Bundle;
import com.unity3d.player.UnityPlayer;
import java.io.InputStream;
import java.io.OutputStream;
import java.io.ByteArrayOutputStream;
import java.nio.charset.StandardCharsets;

/** Android Storage Access Framework integration. No broad storage permissions. */
public class DocumentBridge extends Fragment {
    private static final int REQUEST=5317;
    public static void exportBackup(final Activity activity,final String text) { launch(activity,text,true); }
    public static void importBackup(final Activity activity) { launch(activity,"",false); }
    private static void launch(final Activity activity,final String text,final boolean exporting) {
        activity.runOnUiThread(()->{
            if(activity.getFragmentManager().findFragmentByTag("habit_backup")!=null)return;
            DocumentBridge fragment=new DocumentBridge();Bundle args=new Bundle();args.putString("text",text);args.putBoolean("exporting",exporting);fragment.setArguments(args);
            activity.getFragmentManager().beginTransaction().add(fragment,"habit_backup").commit();
        });
    }
    @Override public void onCreate(Bundle state) {
        super.onCreate(state);
        if(state!=null)return;
        boolean exporting=getArguments().getBoolean("exporting");
        Intent intent=new Intent(exporting?Intent.ACTION_CREATE_DOCUMENT:Intent.ACTION_OPEN_DOCUMENT);
        intent.addCategory(Intent.CATEGORY_OPENABLE);intent.setType("application/json");
        if(exporting)intent.putExtra(Intent.EXTRA_TITLE,"HabitGarden-backup.json");
        startActivityForResult(intent,REQUEST);
    }
    @Override public void onActivityResult(int request,int result,Intent data) {
        if(request!=REQUEST)return;
        try {
            if(result==Activity.RESULT_OK&&data!=null&&data.getData()!=null) {
                if(getArguments().getBoolean("exporting")) {
                    try(OutputStream out=getActivity().getContentResolver().openOutputStream(data.getData(),"wt")) {
                        if(out==null)throw new java.io.IOException("Cannot open backup destination");
                        out.write(getArguments().getString("text").getBytes(StandardCharsets.UTF_8));
                    }
                    UnityPlayer.UnitySendMessage("Habit Garden App","OnBackupFinished","Backup exported.");
                } else {
                    try(InputStream in=getActivity().getContentResolver().openInputStream(data.getData());ByteArrayOutputStream out=new ByteArrayOutputStream()) {
                        if(in==null)throw new java.io.IOException("Cannot open backup");
                        byte[] buffer=new byte[8192];int n;
                        while((n=in.read(buffer))!=-1){if(out.size()+n>10*1024*1024)throw new java.io.IOException("Backup too large");out.write(buffer,0,n);}
                        UnityPlayer.UnitySendMessage("Habit Garden App","OnBackupImported",new String(out.toByteArray(),StandardCharsets.UTF_8));
                    }
                }
            }
        } catch(Exception error) {UnityPlayer.UnitySendMessage("Habit Garden App","OnBackupFinished","Could not read or write the selected backup.");}
        finally {getFragmentManager().beginTransaction().remove(this).commitAllowingStateLoss();}
    }
}
